using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using PetJourneyTutorApi.Security;
namespace PetJourney.UnitTests.Application;
public class AuthServiceTests
{
    private static AuthService Create(IAccountStore store) => new(store, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
        ["Jwt:Issuer"]="PetJourney", ["Jwt:Audience"]="PetJourney", ["Jwt:SigningKey"]="unit-tests-key-with-at-least-32-bytes"
    }).Build(), TimeProvider.System);
    [Theory]
    [InlineData("ADMIN_SISTEMA", null, null, null, true)]
    [InlineData("ADMIN_CLINICA", 1, null, null, true)]
    [InlineData("ADMIN_CLINICA", null, null, null, false)]
    [InlineData("VETERINARIO", 1, null, 1, true)]
    [InlineData("VETERINARIO", 1, null, null, false)]
    [InlineData("TUTOR", null, 1, null, true)]
    [InlineData("TUTOR", 1, 1, null, false)]
    [InlineData("ROOT", null, null, null, false)]
    public void ValidScope_ContaConfigurada_ValidaPerfil(string role, int? clinic, int? tutor, int? vet, bool expected)
    {
        // Arrange
        var account = new Account { Role=role, ClinicId=clinic, TutorId=tutor, VeterinarianId=vet };
        // Act
        var result = AuthService.ValidScope(account);
        // Assert
        Assert.Equal(expected, result);
    }
    [Fact]
    public void Login_UsuarioInexistente_RetornaNulo()
    {
        // Arrange
        var store = new Mock<IAccountStore>();
        store.Setup(x => x.Find("ausente")).Returns((Account?)null);
        // Act
        var result = Create(store.Object).Login(" ausente ", "123456");
        // Assert
        Assert.Null(result); store.Verify(x => x.Find("ausente"), Times.Once);
    }
    [Theory]
    [InlineData("errada", false)]
    [InlineData("123456", true)]
    public void Login_SenhaInformada_EmiteTokenSomenteSeCorreta(string password, bool expected)
    {
        // Arrange
        var account = new Account { Username="tutor@teste.local", Role="TUTOR", TutorId=7 };
        account.PasswordHash = new PasswordHasher<Account>().HashPassword(account, "123456");
        var store = new Mock<IAccountStore>(); store.Setup(x => x.Find(account.Username)).Returns(account);
        // Act
        var result = Create(store.Object).Login(account.Username, password);
        // Assert
        Assert.Equal(expected, result is not null);
        if (result is not null) {
            var json = JsonSerializer.SerializeToElement(result);
            var token = new JwtSecurityTokenHandler().ReadJwtToken(json.GetProperty("token").GetString());
            Assert.Contains(token.Claims, c => c.Type=="tutorId" && c.Value=="7");
            Assert.Equal("PetJourney", token.Issuer);
        }
    }
}
