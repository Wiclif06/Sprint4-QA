using PetJourneyTutorApi.Errors;
using PetJourneyTutorApi.Security;
using PetJourneyTutorApi.Domain;
using PetJourneyTutorApi.Observability;
using PetJourneyTutorApi.Persistence;

using PetJourneyTutorApi.Models;

namespace PetJourneyTutorApi.Services;

public partial class TutorService
{
    private readonly IDataSession _context;

    private readonly ICurrentUser _user;
    private readonly TimeProvider _time;

    public TutorService(IDataSession context, ICurrentUser user, TimeProvider time)
    {
        _context = context;
        _user = user;
        _time = time;
    }

    private IQueryable<Tutor> AccessibleTutors() => _user.Role switch
    {
        "ADMIN_SISTEMA" => _context.Repository<Tutor>().Query,
        "TUTOR" when _user.TutorId.HasValue => _context.Repository<Tutor>().Query.Where(t => t.IdTutor == _user.TutorId.Value),
        "ADMIN_CLINICA" or "VETERINARIO" when _user.ClinicId.HasValue => _context.Repository<Tutor>().Query.Where(t =>
            _context.Repository<TutorClinic>().Query.Any(l => l.TutorId == t.IdTutor && l.ClinicId == _user.ClinicId.Value)),
        _ => _context.Repository<Tutor>().Query.Where(t => false)
    };

    public async Task<List<Tutor>> GetAllAsync()
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.GetAllAsync");
        return await _context.Repository<Tutor>().ListAsync(AccessibleTutors().OrderBy(t => t.IdTutor));
    }

    public async Task<Tutor?> GetByIdAsync(int id)
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.GetByIdAsync");
        return await _context.Repository<Tutor>().FirstAsync(t => t.IdTutor == id);
    }

    public async Task<List<Pet>?> GetPetsByTutorAsync(int id)
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.GetPetsByTutorAsync");
        var tutorCount = await _context.Repository<Tutor>().CountAsync(AccessibleTutors().Where(t => t.IdTutor == id));

        if (tutorCount == 0)
            return null;

        return await _context.Repository<Pet>().ListAsync(_context.Repository<Pet>().Query
            .Where(p => p.IdTutor == id && (_user.Role == "ADMIN_SISTEMA" || _user.Role == "TUTOR" || p.IdClinica == _user.ClinicId))
            .OrderBy(p => p.NmPet)
            );
    }

    public async Task<List<Clinic>?> GetTutorClinicsAsync(int tutorId)
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.GetTutorClinicsAsync");
        var tutorCount = await _context.Repository<Tutor>().CountAsync(AccessibleTutors().Where(t => t.IdTutor == tutorId));

        if (tutorCount == 0)
            return null;

        var clinicIds = await _context.Repository<TutorClinic>().ListAsync(_context.Repository<TutorClinic>().Query.Where(l => l.TutorId == tutorId).Select(l => l.ClinicId));

        return await _context.Repository<Clinic>().ListAsync(_context.Repository<Clinic>().Query
            .Where(c => clinicIds.Contains(c.IdClinica))
            .OrderBy(c => c.NmClinica)
            );
    }

    public async Task<(bool Success, string? Error, Tutor? Tutor)> CreateAsync(Tutor tutor)
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.CreateAsync");
        var emailCount = await _context.Repository<Tutor>().CountAsync(_context.Repository<Tutor>().Query.Where(t => t.DsEmail == tutor.DsEmail));

        if (emailCount > 0)
            return (false, "Já existe um tutor cadastrado com este e-mail.", null);

        tutor.DtCadastro = _time.GetUtcNow().UtcDateTime;
        tutor.IdTutor = 0;

        if (string.IsNullOrWhiteSpace(tutor.DsPlano))
            tutor.DsPlano = "FREE";

        _context.Repository<Tutor>().Add(tutor);
        if (_user.Role == "ADMIN_CLINICA" && _user.ClinicId.HasValue)
        {
            await using var transaction = await _context.BeginTransactionAsync();
            await _context.SaveChangesAsync();
            _context.Repository<TutorClinic>().Add(new TutorClinic { TutorId = tutor.IdTutor, ClinicId = _user.ClinicId.Value });
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        else await _context.SaveChangesAsync();
        return (true, null, tutor);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, Tutor tutor)
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.UpdateAsync");
        if (id != tutor.IdTutor)
            return (false, "O id da rota é diferente do id enviado no corpo da requisição.");

        var currentTutor = await _context.Repository<Tutor>().FirstAsync(t => t.IdTutor == id);

        if (currentTutor == null)
            return (false, "Tutor não encontrado.");

        var emailCount = await _context.Repository<Tutor>().CountAsync(_context.Repository<Tutor>().Query.Where(t => t.DsEmail == tutor.DsEmail && t.IdTutor != id));

        if (emailCount > 0)
            return (false, "Já existe outro tutor cadastrado com este e-mail.");

        currentTutor.NmTutor = tutor.NmTutor;
        currentTutor.DsEmail = tutor.DsEmail;
        currentTutor.NrTelefone = tutor.NrTelefone;
        currentTutor.DsPlano = string.IsNullOrWhiteSpace(tutor.DsPlano)
            ? currentTutor.DsPlano
            : tutor.DsPlano;

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> LinkToClinicAsync(int tutorId, int clinicaId)
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.LinkToClinicAsync");
        var tutorCount = await _context.Repository<Tutor>().CountAsync(AccessibleTutors().Where(t => t.IdTutor == tutorId));

        if (tutorCount == 0)
            return (false, "Tutor não encontrado.");

        if (!_user.IsSystemAdmin && (_user.Role != "ADMIN_CLINICA" || _user.ClinicId != clinicaId))
            return (false, "Clínica fora do escopo de acesso.");
        var clinicCount = await _context.Repository<Clinic>().CountAsync(_context.Repository<Clinic>().Query.Where(c => c.IdClinica == clinicaId));

        if (clinicCount == 0)
            return (false, "Clínica não encontrada.");

        if (!await _context.Repository<TutorClinic>().AnyAsync(l => l.TutorId == tutorId && l.ClinicId == clinicaId))
            _context.Repository<TutorClinic>().Add(new TutorClinic { TutorId = tutorId, ClinicId = clinicaId });
        var pets = await _context.Repository<Pet>().ListAsync(_context.Repository<Pet>().Query.Where(p => p.IdTutor == tutorId && (p.IdClinica == null || p.IdClinica == clinicaId)));
        foreach (var pet in pets) pet.IdClinica = clinicaId;

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnlinkFromClinicAsync(int tutorId)
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.UnlinkFromClinicAsync");
        var tutorCount = await _context.Repository<Tutor>().CountAsync(AccessibleTutors().Where(t => t.IdTutor == tutorId));

        if (tutorCount == 0)
            return (false, "Tutor não encontrado.");

        var pets = await _context.Repository<Pet>().ListAsync(_context.Repository<Pet>().Query
            .Where(p => p.IdTutor == tutorId && (_user.IsSystemAdmin || p.IdClinica == _user.ClinicId))
            );

        var links = await _context.Repository<TutorClinic>().ListAsync(_context.Repository<TutorClinic>().Query.Where(l => l.TutorId == tutorId && (_user.IsSystemAdmin || l.ClinicId == _user.ClinicId)));
        _context.Repository<TutorClinic>().RemoveRange(links);
        foreach (var pet in pets)
            pet.IdClinica = null;

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        using var activity = Telemetry.Activities.StartActivity("TutorService.DeleteAsync");
        var tutor = await _context.Repository<Tutor>().FirstAsync(t => t.IdTutor == id);

        if (tutor == null)
            return (false, "Tutor não encontrado.");

        var petCount = await _context.Repository<Pet>().CountAsync(_context.Repository<Pet>().Query.Where(p => p.IdTutor == id));

        if (petCount > 0)
            return (false, "Não é possível remover um tutor que possui pets cadastrados.");

        var links = await _context.Repository<TutorClinic>().ListAsync(_context.Repository<TutorClinic>().Query.Where(l => l.TutorId == id));
        _context.Repository<TutorClinic>().RemoveRange(links);
        _context.Repository<Tutor>().Remove(tutor);
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
