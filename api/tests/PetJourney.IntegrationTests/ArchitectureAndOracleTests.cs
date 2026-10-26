using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PetJourneyTutorApi.Data;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Services;
namespace PetJourney.IntegrationTests;
public class ArchitectureAndOracleTests
{
    [Fact]
    public void DomainAndApplication_DoNotReferencePersistenceOrHttpFrameworks()
    {
        var domain = typeof(Pet).Assembly;
        var application = typeof(PetService).Assembly;
        Assert.NotEqual(domain, application);
        foreach (var assembly in new[] { domain, application })
        {
            var names = assembly.GetReferencedAssemblies().Select(a => a.Name ?? "").ToArray();
            Assert.DoesNotContain(names, n => n.Contains("EntityFrameworkCore") || n.StartsWith("MongoDB") || n.StartsWith("Microsoft.AspNetCore") || n == "PetJourney.Infrastructure" || n == "PetJourneyTutorApi");
        }
    }
    [Fact]
    public void OracleModelAndMigrations_GenerateSqlWithoutConnectingToDatabase()
    {
        using var db = new DesignTimeDbContextFactory().CreateDbContext([]);
        Assert.Equal(3, db.Database.GetMigrations().Count());
        var script = db.GetService<IMigrator>().GenerateScript();
        if (Environment.GetEnvironmentVariable("PETJOURNEY_EVIDENCE_DIR") is { Length: > 0 } output)
        {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "oracle-migrations-generated-not-executed.sql"), script);
        }
        Assert.Contains("PJTUTORCLINICA", script);
        Assert.Contains("FK_TBPET_TBTUTOR_IDTUTOR", script);
        var query = db.Pets.OrderBy(p => p.NmPet).ThenBy(p => p.IdPet).Skip(1).Take(10).ToQueryString();
        Assert.Contains("TBPET", query); Assert.Contains("NMPET", query);
        Assert.Contains("OFFSET", query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FETCH", query, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("TBPET", db.Model.FindEntityType(typeof(Pet))!.GetTableName());
    }
    [Fact]
    public void OracleMappings_AreCompatibleWithTheExistingMigrationSnapshot()
    {
        using var db = new DesignTimeDbContextFactory().CreateDbContext([]);
        var design = db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        snapshot = db.GetService<Microsoft.EntityFrameworkCore.Infrastructure.IModelRuntimeInitializer>().Initialize(snapshot, designTime: true);
        var differences = db.GetService<IMigrationsModelDiffer>().GetDifferences(snapshot.GetRelationalModel(), design.GetRelationalModel());
        Assert.Empty(differences);
        var pet = design.FindEntityType(typeof(Pet))!;
        Assert.Equal("IDPET", pet.FindProperty(nameof(Pet.IdPet))!.GetColumnName(Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table("TBPET", null)));
    }

}
