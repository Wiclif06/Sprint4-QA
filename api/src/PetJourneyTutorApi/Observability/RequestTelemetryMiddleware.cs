using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Serilog.Context;
namespace PetJourneyTutorApi.Observability;
public sealed class RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers["X-Correlation-ID"].ToString();
        var correlation = Regex.IsMatch(incoming, "^[a-zA-Z0-9_-]{1,64}$") ? incoming : Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = correlation;
        context.Response.OnStarting(() => { context.Response.Headers["X-Correlation-ID"] = correlation; return Task.CompletedTask; });
        var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
        var started = Stopwatch.GetTimestamp();
        using var c = LogContext.PushProperty("CorrelationId", correlation);
        using var t = LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString());
        using var span = Telemetry.Activities.StartActivity("application.request");
        try { await next(context); }
        finally
        {

            var status = context.Response.StatusCode;
            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var tags = new TagList { { "http.route", route }, { "http.request.method", context.Request.Method }, { "http.response.status_code", status } };
            Telemetry.Requests.Add(1, tags); Telemetry.Duration.Record(elapsed, tags);
            if (status >= 500) { Telemetry.Errors.Add(1, tags); span?.SetStatus(ActivityStatusCode.Error); }
            if (status >= 500) logger.LogError("HTTP {Method} {Route} retornou {StatusCode} em {ElapsedMs} ms", context.Request.Method, route, status, elapsed);
            else if (status >= 400) logger.LogWarning("HTTP {Method} {Route} retornou {StatusCode} em {ElapsedMs} ms", context.Request.Method, route, status, elapsed);
            else logger.LogInformation("HTTP {Method} {Route} retornou {StatusCode} em {ElapsedMs} ms", context.Request.Method, route, status, elapsed);
        }
    }
}
