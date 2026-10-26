using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Queries;
using PetJourneyTutorApi.Persistence;
using PetJourneyTutorApi.Observability;
namespace PetJourneyTutorApi.Services;
public partial class PetService
{
    public async Task<PageResult<Pet>> SearchAsync(PetQuery options, CancellationToken ct = default)
    {
        using var activity = Telemetry.Activities.StartActivity("PetService.GetAllAsync");
        Paging.Validate(options);
        var q = AccessiblePets();
        if (!string.IsNullOrWhiteSpace(options.Name)) q = q.Where(p => p.NmPet.Contains(options.Name));
        if (options.TutorId.HasValue) q = q.Where(p => p.IdTutor == options.TutorId);
        if (options.ClinicId.HasValue) q = q.Where(p => p.IdClinica == options.ClinicId);
        if (options.SpeciesId.HasValue) q = q.Where(p => p.IdEspecie == options.SpeciesId);
        if (options.BreedId.HasValue) q = q.Where(p => p.IdRaca == options.BreedId);
        if (options.Sex != null) q = q.Where(p => p.DsSexo == options.Sex);
        var ordered = (options.SortBy, options.Descending) switch
        {
            ("name", false) => q.OrderBy(p => p.NmPet), ("name", true) => q.OrderByDescending(p => p.NmPet),
            ("birthDate", false) => q.OrderBy(p => p.DtNascimento), ("birthDate", true) => q.OrderByDescending(p => p.DtNascimento),
            (_, true) => q.OrderByDescending(p => p.IdPet), _ => q.OrderBy(p => p.IdPet)
        };
        return await Paging.ReadAsync(_context.Repository<Pet>(), ordered.ThenBy(p => p.IdPet), options, ct);
    }
}
public partial class ReminderService
{
    public async Task<PageResult<Reminder>> SearchAsync(ReminderQuery options, CancellationToken ct = default)
    {
        Paging.Validate(options);
        var q = AccessibleReminders();
        if (options.PetId.HasValue) q = q.Where(r => r.IdPet == options.PetId);
        if (options.Status != null) q = q.Where(r => r.DsStatus == options.Status);
        if (!string.IsNullOrWhiteSpace(options.Type)) q = q.Where(r => r.DsTipo == options.Type);
        if (options.From.HasValue) q = q.Where(r => r.DtLembrete >= options.From);
        if (options.To.HasValue) q = q.Where(r => r.DtLembrete <= options.To);
        var ordered = (options.SortBy, options.Descending) switch
        {
            ("id", false) => q.OrderBy(r => r.IdLembrete), ("id", true) => q.OrderByDescending(r => r.IdLembrete),
            ("type", false) => q.OrderBy(r => r.DsTipo), ("type", true) => q.OrderByDescending(r => r.DsTipo),
            ("status", false) => q.OrderBy(r => r.DsStatus), ("status", true) => q.OrderByDescending(r => r.DsStatus),
            (_, true) => q.OrderByDescending(r => r.DtLembrete), _ => q.OrderBy(r => r.DtLembrete)
        };
        return await Paging.ReadAsync(_context.Repository<Reminder>(), ordered.ThenBy(r => r.IdLembrete), options, ct);
    }
}
public partial class TutorService
{
    public async Task<PageResult<Tutor>> SearchAsync(TutorQuery options, CancellationToken ct = default)
    {
        Paging.Validate(options);
        var q = AccessibleTutors();
        if (!string.IsNullOrWhiteSpace(options.Name)) q = q.Where(t => t.NmTutor.Contains(options.Name));
        if (!string.IsNullOrWhiteSpace(options.Email)) q = q.Where(t => t.DsEmail == options.Email);
        if (!string.IsNullOrWhiteSpace(options.Plan)) q = q.Where(t => t.DsPlano == options.Plan);
        var ordered = (options.SortBy, options.Descending) switch
        {
            ("name", false) => q.OrderBy(t => t.NmTutor), ("name", true) => q.OrderByDescending(t => t.NmTutor),
            ("email", false) => q.OrderBy(t => t.DsEmail), ("email", true) => q.OrderByDescending(t => t.DsEmail),
            (_, true) => q.OrderByDescending(t => t.IdTutor), _ => q.OrderBy(t => t.IdTutor)
        };
        return await Paging.ReadAsync(_context.Repository<Tutor>(), ordered.ThenBy(t => t.IdTutor), options, ct);
    }
}
public partial class ClinicService
{
    public async Task<PageResult<Clinic>> SearchAsync(CatalogQuery options, CancellationToken ct = default)
    {
        Paging.Validate(options);
        var q = _context.Repository<Clinic>().Query;
        if (!string.IsNullOrWhiteSpace(options.Name)) q = q.Where(c => c.NmClinica.Contains(options.Name));
        if (!string.IsNullOrWhiteSpace(options.Status)) q = q.Where(c => c.DsStatus == options.Status);
        var ordered = (options.SortBy, options.Descending) switch
        {
            ("name", false) => q.OrderBy(c => c.NmClinica), ("name", true) => q.OrderByDescending(c => c.NmClinica),
            (_, true) => q.OrderByDescending(c => c.IdClinica), _ => q.OrderBy(c => c.IdClinica)
        };
        return await Paging.ReadAsync(_context.Repository<Clinic>(), ordered.ThenBy(c => c.IdClinica), options, ct);
    }
}
public sealed class CatalogService(IDataSession data)
{
    public async Task<PageResult<Species>> SpeciesAsync(CatalogQuery options, CancellationToken ct = default)
    {
        Paging.Validate(options);
        var repo = data.Repository<Species>(); var q = repo.Query;
        if (!string.IsNullOrWhiteSpace(options.Name)) q = q.Where(x => x.NmEspecie.Contains(options.Name));
        var ordered = (options.SortBy, options.Descending) switch
        {
            ("name", false) => q.OrderBy(x => x.NmEspecie), ("name", true) => q.OrderByDescending(x => x.NmEspecie),
            (_, true) => q.OrderByDescending(x => x.IdEspecie), _ => q.OrderBy(x => x.IdEspecie)
        };
        return await Paging.ReadAsync(repo, ordered.ThenBy(x => x.IdEspecie), options, ct);
    }
    public Task<Species?> SpeciesByIdAsync(int id, CancellationToken ct = default) => data.Repository<Species>().FirstAsync(x => x.IdEspecie == id, ct);
    public Task<Breed?> BreedByIdAsync(int id, CancellationToken ct = default) => data.Repository<Breed>().FirstAsync(x => x.IdRaca == id, ct);
    public async Task<PageResult<Breed>> BreedsAsync(CatalogQuery options, CancellationToken ct = default)
    {
        Paging.Validate(options);
        var repo = data.Repository<Breed>(); var q = repo.Query;
        if (!string.IsNullOrWhiteSpace(options.Name)) q = q.Where(x => x.NmRaca.Contains(options.Name));
        if (options.SpeciesId.HasValue) q = q.Where(x => x.IdEspecie == options.SpeciesId);
        var ordered = (options.SortBy, options.Descending) switch
        {
            ("name", false) => q.OrderBy(x => x.NmRaca), ("name", true) => q.OrderByDescending(x => x.NmRaca),
            (_, true) => q.OrderByDescending(x => x.IdRaca), _ => q.OrderBy(x => x.IdRaca)
        };
        return await Paging.ReadAsync(repo, ordered.ThenBy(x => x.IdRaca), options, ct);
    }
}
