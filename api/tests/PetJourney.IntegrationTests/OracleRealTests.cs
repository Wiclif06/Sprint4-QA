using Microsoft.EntityFrameworkCore;
using PetJourneyTutorApi.Data;
using PetJourneyTutorApi.Security;
namespace PetJourney.IntegrationTests;
public sealed class OracleRealFactAttribute : FactAttribute
{
    public OracleRealFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PETJOURNEY_TEST_ORACLE")))
            Skip = "Defina PETJOURNEY_TEST_ORACLE para um schema de testes com as migrações aplicadas.";
    }
}
public class OracleRealTests
{
    [OracleRealFact]
    public async Task OracleSchema_ConnectsHasNoPendingMigrationsAndReadsMappedTables()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseOracle(Environment.GetEnvironmentVariable("PETJOURNEY_TEST_ORACLE")).Options;
        await using var db = new AppDbContext(options, new TestAdmin());
        Assert.True(await db.Database.CanConnectAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        await db.Species.Take(1).ToListAsync();
        await db.Breeds.Take(1).ToListAsync();
        await db.Tutors.Take(1).ToListAsync();
        await db.Clinics.Take(1).ToListAsync();
        await db.Pets.OrderBy(x => x.IdPet).Skip(1).Take(1).ToListAsync();
        await db.Reminders.Take(1).ToListAsync();
        await db.TutorClinics.Take(1).ToListAsync();
    }
    private sealed class TestAdmin : ICurrentUser
    {
        public string Username => "oracle-integration-test";
        public string Role => "ADMIN_SISTEMA";
        public int? TutorId => null;
        public int? ClinicId => null;
        public bool IsSystemAdmin => true;
    }
}
