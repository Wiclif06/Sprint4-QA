using PetJourneyTutorApi.Queries;
namespace PetJourneyTutorApi.Mongo;
public sealed class ClinicalEventQuery : PageQuery
{
    public ClinicalEventQuery() { SortBy = "date"; Descending = true; }
    [System.ComponentModel.DataAnnotations.MaxLength(60)] public string? Type { get; set; }
    protected override string[] AllowedSorts => ["id", "date", "type"];
}
public interface IClinicalEventRepository
{
    Task<PageResult<ClinicalEvent>> SearchAsync(int petId, ClinicalEventQuery options, CancellationToken ct);
    Task<ClinicalEvent?> GetAsync(int petId, string id, CancellationToken ct);
    Task<ClinicalEvent> AddAsync(ClinicalEvent item, CancellationToken ct);
}
