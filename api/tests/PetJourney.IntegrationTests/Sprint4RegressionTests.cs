using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PetJourneyTutorApi;
using PetJourneyTutorApi.Data;
using PetJourneyTutorApi.Errors;
using PetJourneyTutorApi.Mongo;
using PetJourneyTutorApi.Queries;
namespace PetJourney.IntegrationTests;
[Collection("API")]
public class Sprint4RegressionTests(ApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;
    [Theory]
    [InlineData("/api/pets?page=0")][InlineData("/api/pets?pageSize=101")]
    [InlineData("/api/pets?sortBy=untrusted")][InlineData("/api/pets?speciesId=-1")]
    [InlineData("/api/pets?sex=X")][InlineData("/api/lembretes?status=INVALIDO")]
    [InlineData("/api/lembretes?from=2026-12-01&to=2026-01-01")]
    [InlineData("/api/tutores?pageSize=0")][InlineData("/api/clinicas?sortBy=email")]
    [InlineData("/api/racas?speciesId=0")][InlineData("/api/especies?page=-1")]
    public async Task InvalidQuery_Returns400(string path)
    {
        using var client = await fixture.LoginAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(path)).StatusCode);
    }
    [Theory]
    [InlineData("/api/pets")][InlineData("/api/lembretes")][InlineData("/api/tutores")]
    [InlineData("/api/clinicas")][InlineData("/api/especies")][InlineData("/api/racas")]
    public async Task DistantPage_ReturnsEmptyArrayWithoutOverflow(string path)
    {
        using var client = await fixture.LoginAsync("admin.sistema@petjourney.com");
        var response = await client.GetAsync(path + "?page=2147483647&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<JsonElement[]>())!);
    }
    [Fact]
    public async Task FilterSortAndLinks_PreserveQueryAndScope()
    {
        using var admin = await fixture.LoginAsync("admin.sistema@petjourney.com");
        var response = await admin.GetAsync("/api/pets?name=R&pageSize=1&sortBy=name&descending=true");
        Assert.Equal("1", Assert.Single(response.Headers.GetValues("X-Total-Count")));
        var pet = Assert.Single((await response.Content.ReadFromJsonAsync<JsonElement[]>())!);
        Assert.Equal("Rex", pet.GetProperty("nmPet").GetString());
        Assert.Equal("/api/pets/1", pet.GetProperty("_links").GetProperty("self").GetProperty("href").GetString());
        var links = string.Join(",", response.Headers.GetValues("Link"));
        Assert.Contains("name=R", links); Assert.Contains("sortBy=name", links); Assert.Contains("descending=true", links);
        using var tutor = await fixture.LoginAsync();
        var hidden = await tutor.GetAsync("/api/pets?tutorId=2");
        Assert.Empty((await hidden.Content.ReadFromJsonAsync<JsonElement[]>())!);
        Assert.Equal("0", Assert.Single(hidden.Headers.GetValues("X-Total-Count")));
    }
    [Theory]
    [InlineData("/api/lembretes?type=VACINA", "idLembrete", 1)]
    [InlineData("/api/lembretes?petId=2&status=PENDENTE", "idLembrete", 2)]
    [InlineData("/api/tutores?email=ana%40petjourney.com", "idTutor", 2)]
    [InlineData("/api/clinicas?name=Feliz", "idClinica", 1)]
    [InlineData("/api/especies?name=Gato", "idEspecie", 2)]
    [InlineData("/api/racas?speciesId=1&name=Golden", "idRaca", 1)]
    public async Task Filters_ApplyToAllMainCollections(string path, string property, int id)
    {
        using var client = await fixture.LoginAsync("admin.sistema@petjourney.com");
        var items = await client.GetFromJsonAsync<JsonElement[]>(path);
        Assert.Equal(id, Assert.Single(items!).GetProperty(property).GetInt32());
    }
    [Fact]
    public async Task DatabasePaging_UsesSqlLimitInsteadOfLoadingAllRows()
    {
        var recorder = new SqlRecorder();
        using var scope = fixture.Services.CreateScope();
        var connection = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetDbConnection();
        var actor = new SystemActor();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).AddInterceptors(recorder).Options;
        await using var db = new AppDbContext(options, actor);
        var service = new PetJourneyTutorApi.Services.PetService(new PetJourney.Infrastructure.Persistence.EfDataSession(db), actor, TimeProvider.System);
        var page = await service.SearchAsync(new PetQuery { Page = 2, PageSize = 1, SortBy = "name" });
        Assert.Equal(2, page.Total);
        Assert.Equal(1, Assert.Single(page.Items).IdPet);
        Assert.Contains(recorder.Commands, sql => sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase) && sql.Contains("OFFSET", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(recorder.Commands, sql => sql.Contains("COUNT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EqualSortValues_UseIdAsTieBreaker()
    {
        using (var scope = fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Pets.Add(new PetJourneyTutorApi.Models.Pet { NmPet = "Rex", DsSexo = "M", IdTutor = 1, IdClinica = 1, IdEspecie = 1 });
            await db.SaveChangesAsync();
        }
        using var client = await fixture.LoginAsync();
        var first = await client.GetFromJsonAsync<JsonElement[]>("/api/pets?sortBy=name&pageSize=1&page=1");
        var second = await client.GetFromJsonAsync<JsonElement[]>("/api/pets?sortBy=name&pageSize=1&page=2");
        Assert.Equal(1, Assert.Single(first!).GetProperty("idPet").GetInt32());
        Assert.True(Assert.Single(second!).GetProperty("idPet").GetInt32() > 1);
    }
    [Theory]
    [InlineData("/api/pets/2")][InlineData("/api/tutores/2")][InlineData("/api/clinicas/2")]
    [InlineData("/api/lembretes/2")][InlineData("/api/tutores/2/pets")]
    public async Task Clinic_CannotReadOtherClinic(string path)
    {
        using var client = await fixture.LoginAsync("admin.petfeliz@petjourney.com");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }
    [Theory]
    [InlineData("PUT")][InlineData("DELETE")]
    public async Task Reminder_OutOfScope_CannotBeChanged(string method)
    {
        using var client = await fixture.LoginAsync();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/lembretes/2");
        if (method == "PUT") request.Content = JsonContent.Create(new { idLembrete = 2, idPet = 2, dsTipo = "RETORNO", dsDescricao = "Modificado", dtLembrete = DateTime.UtcNow.AddDays(1), dsStatus = "PENDENTE" });
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(request)).StatusCode);
        using var owner = await fixture.LoginAsync("ana@petjourney.com");
        Assert.Equal("Retorno Mia", (await owner.GetFromJsonAsync<JsonElement>("/api/lembretes/2")).GetProperty("dsDescricao").GetString());
    }
    [Fact]
    public async Task Veterinarian_ReadsSameReminderThroughBothRoutes()
    {
        using var client = await fixture.LoginAsync("dr.joao@petjourney.com");
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/lembretes"))!);
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/pets/1/lembretes"))!);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/lembretes/1")).StatusCode);
    }
    [Theory]
    [InlineData("admin.sistema@petjourney.com")][InlineData("admin.petfeliz@petjourney.com")][InlineData("dr.joao@petjourney.com")]
    public async Task Events_RecordActualAuthorAndResolvableLocation(string username)
    {
        var repository = new MemoryEvents();
        using var factory = WithEvents(repository);
        using var client = await LoginAsync(factory, username);
        var response = await client.PostAsJsonAsync("/api/pets/1/eventos", new { type = " Consulta ", description = " Exame clínico " });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(username, created.GetProperty("createdBy").GetString());
        Assert.Equal("Consulta", created.GetProperty("type").GetString());
        Assert.Equal("Exame clínico", created.GetProperty("description").GetString());
        Assert.NotNull(response.Headers.Location);
        var fetched = await client.GetFromJsonAsync<JsonElement>(response.Headers.Location);
        Assert.Equal(created.GetProperty("id").GetString(), fetched.GetProperty("id").GetString());
        Assert.Equal(response.Headers.Location!.AbsoluteUri, new Uri(client.BaseAddress!, fetched.GetProperty("_links").GetProperty("self").GetProperty("href").GetString()!).AbsoluteUri);
    }
    [Theory]
    [InlineData("/api/pets/2/eventos")][InlineData("/api/pets/2/eventos/000000000000000000000001")]
    public async Task HiddenPet_DoesNotCallEventsRepository(string path)
    {
        var repository = new MemoryEvents { Failure = new InvalidOperationException("must-not-call") };
        using var factory = WithEvents(repository);
        using var client = await LoginAsync(factory, "carlos@petjourney.com");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        Assert.Equal(0, repository.Calls);
    }
    [Theory]
    [InlineData(" a", "valid description")][InlineData("Consulta", " a")]
    public async Task EventContent_IsValidatedAfterTrimming(string type, string description)
    {
        var repository = new MemoryEvents();
        using var factory = WithEvents(repository);
        using var client = await LoginAsync(factory, "dr.joao@petjourney.com");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/pets/1/eventos", new { type, description })).StatusCode);
        Assert.Equal(0, repository.Calls);
    }
    [Theory]
    [InlineData("unavailable",503)][InlineData("conflict",409)][InlineData("unexpected",500)][InlineData("write",500)]
    public async Task ExceptionHandler_ClassifiesAndLogsWithoutExposingDetails(string kind, int expected)
    {
        Exception exception = kind switch
        {
            "unavailable" => new DependencyUnavailableException("secret-marker"),
            "conflict" => new DataConflictException("secret-marker"),
            "write" => new DbUpdateException("secret-marker"), _ => new InvalidOperationException("secret-marker")
        };
        var capture = new ExceptionLogger();
        using var factory = fixture.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.AddSingleton<IClinicalEventRepository>(new MemoryEvents { Failure = exception });
            s.AddSingleton<ILogger<GlobalExceptionHandler>>(capture);
        }));
        using var client = await LoginAsync(factory, "carlos@petjourney.com");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "regression-errors");
        var response = await client.GetAsync("/api/pets/1/eventos");
        Assert.Equal(expected, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("regression-errors", Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret-marker", body);
        Assert.Equal("regression-errors", JsonDocument.Parse(body).RootElement.GetProperty("correlationId").GetString());
        Assert.Contains(exception, capture.Exceptions);
    }

    [Fact]
    public async Task Swagger_DescribesNewQueriesLinksAndClinicalResourceRoute()
    {
        using var factory = fixture.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(doc.GetProperty("paths").TryGetProperty("/api/pets/{petId}/eventos/{eventId}", out _));
        var parameters = doc.GetProperty("paths").GetProperty("/api/pets").GetProperty("get").GetProperty("parameters").EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();
        Assert.Contains(parameters, name => string.Equals(name, "sortBy", StringComparison.OrdinalIgnoreCase));
        Assert.True(doc.GetProperty("components").GetProperty("schemas").GetProperty("Pet").GetProperty("properties").TryGetProperty("_links", out _));
    }
    [Theory]
    [InlineData("/api/pets/1")][InlineData("/api/lembretes/1")][InlineData("/api/tutores/1")]
    [InlineData("/api/clinicas/1")][InlineData("/api/especies/1")][InlineData("/api/racas/1")]
    public async Task ResourceResponses_ExposeResolvableSelfLinks(string path)
    {
        using var client = await fixture.LoginAsync();
        var resource = await client.GetFromJsonAsync<JsonElement>(path);
        var self = resource.GetProperty("_links").GetProperty("self").GetProperty("href").GetString();
        Assert.Equal(path, self);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(self)).StatusCode);
        Assert.False(resource.GetProperty("_links").TryGetProperty("publish", out _));
    }

    private WebApplicationFactory<Program> WithEvents(MemoryEvents repository) =>
        fixture.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<IClinicalEventRepository>(repository)));
    internal static async Task<HttpClient> LoginAsync(WebApplicationFactory<Program> factory, string username)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/auth/login", new { username, password = "123456" });
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new("Bearer", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString());
        return client;
    }
    private sealed class MemoryEvents : IClinicalEventRepository
    {
        public Exception? Failure; public int Calls; private readonly List<ClinicalEvent> _items = [];
        private void Check() { Calls++; if (Failure is not null) throw Failure; }
        public Task<PageResult<ClinicalEvent>> SearchAsync(int petId, ClinicalEventQuery options, CancellationToken ct)
        {
            Check(); var items = _items.Where(x => x.PetId == petId).ToList();
            return Task.FromResult(new PageResult<ClinicalEvent>(items, items.Count, options.Page, options.PageSize));
        }
        public Task<ClinicalEvent?> GetAsync(int petId, string id, CancellationToken ct) { Check(); return Task.FromResult(_items.SingleOrDefault(x => x.PetId == petId && x.Id == id)); }
        public Task<ClinicalEvent> AddAsync(ClinicalEvent item, CancellationToken ct) { Check(); item.Id = (_items.Count + 1).ToString("x24"); _items.Add(item); return Task.FromResult(item); }
    }
    private sealed class ExceptionLogger : ILogger<GlobalExceptionHandler>
    {
        public readonly List<Exception> Exceptions = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? ex, Func<TState, Exception?, string> formatter) { if (ex is not null) Exceptions.Add(ex); }
    }
    private sealed class SystemActor : PetJourneyTutorApi.Security.ICurrentUser
    {
        public string Username => "system";
        public string Role => "ADMIN_SISTEMA";
        public int? TutorId => null;
        public int? ClinicId => null;
        public bool IsSystemAdmin => true;
    }
    private sealed class SqlRecorder : DbCommandInterceptor
    {
        public readonly ConcurrentQueue<string> Commands = new();
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Commands.Enqueue(command.CommandText); return ValueTask.FromResult(result); }
    }
}
