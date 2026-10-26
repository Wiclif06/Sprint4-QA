using Microsoft.Extensions.Diagnostics.HealthChecks;
using PetJourneyTutorApi.Data;
namespace PetJourneyTutorApi.Observability;
public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try { return await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy("Banco acessível.") : HealthCheckResult.Unhealthy("Banco indisponível."); }
        catch (Exception) { return HealthCheckResult.Unhealthy("Falha de conectividade com o banco."); }
    }
}
public sealed class ClinicalApiHealthCheck(IHttpClientFactory clients, IConfiguration config) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(config["ExternalServices:ClinicalApiHealthUrl"], UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            return HealthCheckResult.Unhealthy("Endpoint de saúde da API clínica não configurado.");
        try
        {
            using var response = await clients.CreateClient("clinical-health").GetAsync(uri, cancellationToken);
            return response.IsSuccessStatusCode ? HealthCheckResult.Healthy("API clínica acessível.") : HealthCheckResult.Unhealthy("API clínica retornou falha.");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException) { return HealthCheckResult.Unhealthy("API clínica indisponível ou com timeout."); }
    }
}
