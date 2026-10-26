using System.Text;
using PetJourney.Infrastructure.Persistence;
using PetJourneyTutorApi.Persistence;
using PetJourneyTutorApi.Hypermedia;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Mongo;
using PetJourneyTutorApi;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PetJourneyTutorApi.Data;
using PetJourneyTutorApi.Security;
using PetJourneyTutorApi.Services;
using PetJourneyTutorApi.Observability;
using Serilog;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);
DemoData.Configure(builder);
builder.Host.UseSerilog((ctx, log) => log.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext()
    .WriteTo.Console(new JsonFormatter()).WriteTo.File(new JsonFormatter(), "logs/petjourney-.json", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAccountStore, ConfigurationAccountStore>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.Items["CorrelationId"]?.ToString() ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers(options => options.Filters.Add<ResourceLinksFilter>());
builder.Services.AddScoped<IDataSession, EfDataSession>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<ClinicalEventService>();
builder.Services.AddSingleton<MongoClinicalEventRepository>();
builder.Services.AddSingleton<IClinicalEventRepository>(s => s.GetRequiredService<MongoClinicalEventRepository>());
builder.Services.AddHostedService<MongoIndexInitializer>();
builder.Services.AddScoped<TutorService>(); builder.Services.AddScoped<PetService>();
builder.Services.AddScoped<ReminderService>(); builder.Services.AddScoped<ClinicService>();
builder.Services.AddDbContext<AppDbContext>(options => {
    if (builder.Configuration["Database:Provider"] == "Sqlite" && (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")))
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=petjourney.db");
    else options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection"));
});
builder.Services.AddOptions<JwtSettings>().Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(j => !string.IsNullOrWhiteSpace(j.SigningKey) && Encoding.UTF8.GetByteCount(j.SigningKey) >= 32,
        "Configure Jwt__SigningKey com pelo menos 32 bytes.").ValidateOnStart();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"] ?? "")), ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = builder.Configuration.GetValue<int?>("RateLimit:LoginPermitLimit") ?? 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddHttpClient("clinical-health", c => c.Timeout = TimeSpan.FromSeconds(3));
var health = builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5))
    .AddCheck<ClinicalApiHealthCheck>("clinical-api", tags: new[] { "ready" }, timeout: TimeSpan.FromSeconds(5));
// Resolve final configuration at runtime, including WebApplicationFactory and environment overrides.
builder.Services.AddOptions<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>()
    .Configure<IConfiguration, IHostEnvironment>((options, config, environment) =>
    {
        var required = config.GetValue<bool?>("Mongo:Required") ??
            !(environment.IsDevelopment() || environment.IsEnvironment("Testing"));
        if (required || !string.IsNullOrWhiteSpace(config["Mongo:ConnectionString"]))
            options.Registrations.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
                "mongodb", services => ActivatorUtilities.CreateInstance<MongoHealthCheck>(services),
                null, new[] { "ready" }, TimeSpan.FromSeconds(5)));
    });
builder.Services.AddOpenTelemetry().ConfigureResource(r => r.AddService(Telemetry.Name))
    .WithTracing(t => { t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource(Telemetry.Name);
        if (builder.Configuration.GetValue<bool>("Telemetry:Console")) t.AddConsoleExporter();
        if (!string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"])) t.AddOtlpExporter(); })
    .WithMetrics(m => { m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddMeter(Telemetry.Name);
        if (builder.Configuration.GetValue<bool>("Telemetry:Console")) m.AddConsoleExporter();
        if (!string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"])) m.AddOtlpExporter(); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => {
    o.SchemaFilter<ResourceLinksSchemaFilter>();
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "PetJourney Tutor API", Version = "Sprint 4" });
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PetJourneyTutorApi.xml"));
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() });
});
var app = builder.Build();
await DemoData.SeedAsync(app);
app.UseRouting();
app.UseMiddleware<RequestTelemetryMiddleware>();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (!app.Environment.IsEnvironment("Testing")) app.UseHttpsRedirection();
app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") }).AllowAnonymous();
app.MapHealthChecks("/health", new HealthCheckOptions {
    ResponseWriter = async (context, report) => await context.Response.WriteAsJsonAsync(new {
        status = report.Status.ToString(), durationMs = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description, durationMs = e.Value.Duration.TotalMilliseconds })
    })
}).RequireAuthorization(new AuthorizeAttribute { Roles = "ADMIN_SISTEMA" });
app.MapControllers();
app.Run();
public partial class Program { }

