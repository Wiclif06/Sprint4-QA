using Microsoft.Extensions.Diagnostics.HealthChecks;
using PetJourneyTutorApi.Mongo;
namespace PetJourneyTutorApi.Observability;
public sealed class MongoHealthCheck(MongoClinicalEventRepository events) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try { await events.PingAsync(ct); return HealthCheckResult.Healthy("MongoDB e índice acessíveis."); }
        catch (Exception) when (!ct.IsCancellationRequested) { return HealthCheckResult.Unhealthy("MongoDB indisponível ou não configurado."); }
    }
}
