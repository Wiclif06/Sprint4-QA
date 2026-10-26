using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PetJourneyTutorApi.Security;
namespace PetJourneyTutorApi.Controllers;
public sealed record LoginRequest([Required, MaxLength(150)] string Username, [Required, MaxLength(256)] string Password);
[ApiController]
public sealed class AuthController(AuthService auth, ICurrentUser user) : ControllerBase
{
    [AllowAnonymous, HttpPost("auth/login"), HttpPost("login"), EnableRateLimiting("login")]
    public IActionResult Login(LoginRequest input) => auth.Login(input.Username, input.Password) is { } result ? Ok(result) : Unauthorized(new { message = "Credenciais inválidas." });
    [Authorize, HttpGet("auth/me")]
    public IActionResult Me() => Ok(new { username = User.FindFirstValue(ClaimTypes.NameIdentifier), role = user.Role, clinicId = user.ClinicId, tutorId = user.TutorId, veterinarianId = int.TryParse(User.FindFirstValue("veterinarianId"), out var id) ? (int?)id : null });
}
