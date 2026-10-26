using PetJourneyTutorApi.Observability;
using PetJourneyTutorApi.Persistence;

using PetJourneyTutorApi.Models;

namespace PetJourneyTutorApi.Services;

public partial class ClinicService
{
    private readonly IDataSession _context;

    public ClinicService(IDataSession context)
    {
        _context = context;
    }

    public async Task<List<Clinic>> GetAllAsync()
    {
        using var activity = Telemetry.Activities.StartActivity("ClinicService.GetAllAsync");
        return await _context.Repository<Clinic>().ListAsync(_context.Repository<Clinic>().Query
            .OrderBy(c => c.NmClinica)
            );
    }

    public async Task<Clinic?> GetByIdAsync(int id)
    {
        using var activity = Telemetry.Activities.StartActivity("ClinicService.GetByIdAsync");
        return await _context.Repository<Clinic>().FirstAsync(c => c.IdClinica == id);
    }
}
