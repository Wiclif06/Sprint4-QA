namespace PetJourneyTutorApi.Mongo;
public sealed class ClinicalEvent
{
    public string? Id { get; set; }
    public int PetId { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = "";
}
