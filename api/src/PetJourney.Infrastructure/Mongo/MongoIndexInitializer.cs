using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PetJourneyTutorApi.Errors;
namespace PetJourneyTutorApi.Mongo;
public sealed class MongoIndexInitializer(MongoClinicalEventRepository events, IConfiguration config, ILogger<MongoIndexInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(config["Mongo:ConnectionString"])) return;
        try { await events.InitializeAsync(stoppingToken); }
        catch (DependencyUnavailableException ex) { logger.LogWarning(ex, "Índice Mongo pendente; readiness e próximas operações tentarão novamente"); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
