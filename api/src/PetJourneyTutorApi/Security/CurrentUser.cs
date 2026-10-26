using System.Security.Claims;
namespace PetJourneyTutorApi.Security;
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public string Username => Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    public string Role => Principal?.FindFirstValue(ClaimTypes.Role) ?? "";
    private int? Id(string name) => int.TryParse(Principal?.FindFirstValue(name), out var id) ? id : null;
    public int? TutorId => Id("tutorId");
    public int? ClinicId => Id("clinicId");
    public bool IsSystemAdmin => Role == "ADMIN_SISTEMA";
}
