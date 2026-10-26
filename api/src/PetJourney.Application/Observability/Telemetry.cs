using System.Diagnostics;
using System.Diagnostics.Metrics;
namespace PetJourneyTutorApi.Observability;
public static class Telemetry
{
    public const string Name = "PetJourneyTutorApi";
    public static readonly ActivitySource Activities = new(Name);
    public static readonly Meter Meter = new(Name);
    public static readonly Counter<long> Requests = Meter.CreateCounter<long>("petjourney.requests");
    public static readonly Counter<long> Errors = Meter.CreateCounter<long>("petjourney.errors");
    public static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("petjourney.request.duration", "ms");
}
