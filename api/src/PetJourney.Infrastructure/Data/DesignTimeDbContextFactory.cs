using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PetJourneyTutorApi.Security;
namespace PetJourneyTutorApi.Data;
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseOracle(
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__OracleConnection")) ? "User Id=design;Password=design;Data Source=localhost:1521/FREEPDB1" : Environment.GetEnvironmentVariable("ConnectionStrings__OracleConnection")).Options;
        return new AppDbContext(options, new DesignUser());
    }
    private sealed class DesignUser : ICurrentUser
    {
        public string Username => "design";
        public string Role => "ADMIN_SISTEMA";
        public int? TutorId => null;
        public int? ClinicId => null;
        public bool IsSystemAdmin => true;
    }
}

