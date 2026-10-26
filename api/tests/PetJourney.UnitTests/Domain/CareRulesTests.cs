using PetJourneyTutorApi.Domain;
namespace PetJourney.UnitTests.Domain;
public class CareRulesTests
{
    [Theory]
    [InlineData("Rex", "M", 2020, true)]
    [InlineData("", "M", 2020, false)]
    [InlineData("Rex", "X", 2020, false)]
    [InlineData("Rex", "F", 2030, false)]
    public void ValidatePet_DadosInformados_RespeitaRegras(string name, string sex, int year, bool valid)
    {
        // Arrange
        var today = new DateTime(2026, 9, 7);
        // Act
        var error = CareRules.ValidatePet(name, sex, new DateTime(year, 1, 1), today);
        // Assert
        Assert.Equal(valid, error is null);
    }
    [Theory]
    [InlineData(-1, "PENDENTE", false)]
    [InlineData(0, "PENDENTE", true)]
    [InlineData(1, "CONCLUIDO", true)]
    [InlineData(1, "CANCELADO", true)]
    [InlineData(1, "INVALIDO", false)]
    public void ValidateReminder_DataEStatus_ValidaLimites(int offset, string status, bool valid)
    {
        // Arrange
        var today = new DateTime(2026, 9, 7);
        // Act
        var result = CareRules.ValidateReminder(today.AddDays(offset), status, today);
        // Assert
        Assert.Equal(valid, result is null);
    }
    [Theory]
    [InlineData("TUTOR", 1, null, 1, 2, true)]
    [InlineData("TUTOR", 1, null, 2, 2, false)]
    [InlineData("TUTOR", null, null, 1, 2, false)]
    [InlineData("ADMIN_CLINICA", null, 1, 1, 1, true)]
    [InlineData("ADMIN_CLINICA", null, 1, 1, 2, false)]
    [InlineData("VETERINARIO", null, 1, 1, 1, false)]
    [InlineData("ADMIN_SISTEMA", null, null, 1, 2, true)]
    [InlineData("", null, null, 1, 2, false)]
    public void CanOwnPet_PerfilEEscopo_ImpedeAcessoIndevido(string role, int? tutor, int? clinic, int petTutor, int? petClinic, bool expected)
    {
        // Arrange
        var expectedAccess = expected;
        // Act
        var actual = CareRules.CanOwnPet(role, tutor, clinic, petTutor, petClinic);
        // Assert
        Assert.Equal(expectedAccess, actual);
    }
}
