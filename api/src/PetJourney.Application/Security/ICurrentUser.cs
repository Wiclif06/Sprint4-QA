namespace PetJourneyTutorApi.Security;
public interface ICurrentUser
{
    string Role { get; }
    string Username { get; }
    int? TutorId { get; }
    int? ClinicId { get; }
    bool IsSystemAdmin { get; }
}
