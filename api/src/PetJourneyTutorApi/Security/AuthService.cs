using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
namespace PetJourneyTutorApi.Security;
public sealed class Account
{
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "";
    public int? ClinicId { get; set; }
    public int? TutorId { get; set; }
    public int? VeterinarianId { get; set; }
}
public interface IAccountStore { Account? Find(string username); }
public sealed class ConfigurationAccountStore(IConfiguration config) : IAccountStore
{
    public Account? Find(string username) => config.GetSection("Accounts").Get<Account[]>()?
        .SingleOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
}
public sealed class AuthService(IAccountStore accounts, IConfiguration config, TimeProvider time)
{
    public object? Login(string username, string password)
    {
        var account = accounts.Find(username.Trim());
        if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash)) return null;
        PasswordVerificationResult verified;
        try { verified = new PasswordHasher<Account>().VerifyHashedPassword(account, account.PasswordHash, password); }
        catch (FormatException) { return null; }
        if (verified == PasswordVerificationResult.Failed || !ValidScope(account)) return null;
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, account.Username), new(ClaimTypes.Role, account.Role), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        if (account.ClinicId.HasValue) claims.Add(new("clinicId", account.ClinicId.Value.ToString()));
        if (account.TutorId.HasValue) claims.Add(new("tutorId", account.TutorId.Value.ToString()));
        if (account.VeterinarianId.HasValue) claims.Add(new("veterinarianId", account.VeterinarianId.Value.ToString()));
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims,
            notBefore: time.GetUtcNow().UtcDateTime, expires: time.GetUtcNow().AddHours(1).UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"]!)), SecurityAlgorithms.HmacSha256));
        return new { token = new JwtSecurityTokenHandler().WriteToken(token), role = account.Role, clinicId = account.ClinicId, tutorId = account.TutorId, veterinarianId = account.VeterinarianId };
    }
    public static bool ValidScope(Account a) => a.Role switch
    {
        "ADMIN_SISTEMA" => a.ClinicId is null && a.TutorId is null && a.VeterinarianId is null,
        "ADMIN_CLINICA" => a.ClinicId > 0 && a.TutorId is null && a.VeterinarianId is null,
        "VETERINARIO" => a.ClinicId > 0 && a.VeterinarianId > 0 && a.TutorId is null,
        "TUTOR" => a.TutorId > 0 && a.ClinicId is null && a.VeterinarianId is null,
        _ => false
    };
}
