using PetJourneyTutorApi.Security;
using PetJourneyTutorApi.Domain;
using PetJourneyTutorApi.Observability;
using PetJourneyTutorApi.Persistence;

using PetJourneyTutorApi.Models;

namespace PetJourneyTutorApi.Services;

public partial class PetService
{
    private readonly IDataSession _context;

    private readonly ICurrentUser _user;
    private readonly TimeProvider _time;

    public PetService(IDataSession context, ICurrentUser user, TimeProvider time)
    {
        _context = context;
        _user = user;
        _time = time;
    }

    // Filtro no banco: nenhuma leitura nem mutação consulta pets fora do escopo do JWT.
    private IQueryable<Pet> AccessiblePets() => _user.Role switch
    {
        "ADMIN_SISTEMA" => _context.Repository<Pet>().Query,
        "TUTOR" when _user.TutorId.HasValue => _context.Repository<Pet>().Query.Where(p => p.IdTutor == _user.TutorId.Value),
        "ADMIN_CLINICA" or "VETERINARIO" when _user.ClinicId.HasValue => _context.Repository<Pet>().Query.Where(p => p.IdClinica == _user.ClinicId.Value),
        _ => _context.Repository<Pet>().Query.Where(p => false)
    };

    public async Task<List<Pet>> GetAllAsync()
    {
        using var activity = Telemetry.Activities.StartActivity("PetService.GetAllAsync");
        return await _context.Repository<Pet>().ListAsync(AccessiblePets().OrderBy(p => p.IdPet));
    }

    public async Task<Pet?> GetByIdAsync(int id)
    {
        using var activity = Telemetry.Activities.StartActivity("PetService.GetByIdAsync");
        return await _context.Repository<Pet>().FirstAsync(p => p.IdPet == id);
    }

    public async Task<List<Reminder>?> GetRemindersByPetAsync(int petId)
    {
        using var activity = Telemetry.Activities.StartActivity("PetService.GetRemindersByPetAsync");
        var petCount = await _context.Repository<Pet>().CountAsync(AccessiblePets().Where(p => p.IdPet == petId));

        if (petCount == 0)
            return null;

        return await _context.Repository<Reminder>().ListAsync(_context.Repository<Reminder>().Query
            .Where(r => r.IdPet == petId)
            .OrderBy(r => r.DtLembrete)
            );
    }

    public async Task<(bool Success, string? Error, List<TimelineItem>? Timeline)> GetTimelineAsync(int petId, int mes, int ano)
    {
        using var activity = Telemetry.Activities.StartActivity("PetService.GetTimelineAsync");
        if (mes < 1 || mes > 12)
            return (false, "O mês deve estar entre 1 e 12.", null);

        if (ano < 2000 || ano > 9998)
            return (false, "Informe um ano válido.", null);

        var petCount = await _context.Repository<Pet>().CountAsync(AccessiblePets().Where(p => p.IdPet == petId));

        if (petCount == 0)
            return (false, "Pet não encontrado.", null);

        var timeline = await _context.Repository<Reminder>().ListAsync(_context.Repository<Reminder>().Query
            .Where(r => r.IdPet == petId &&
                        r.DtLembrete.Month == mes &&
                        r.DtLembrete.Year == ano)
            .OrderBy(r => r.DtLembrete)
            .Select(r => new TimelineItem
            {
                Tipo = r.DsTipo,
                Descricao = r.DsDescricao,
                Data = r.DtLembrete,
                Status = r.DsStatus
            })
            );

        return (true, null, timeline);
    }

    public async Task<(bool Success, string? Error, Pet? Pet)> CreateAsync(Pet pet)
    {
        using var activity = Telemetry.Activities.StartActivity("PetService.CreateAsync");
        if (!CareRules.CanOwnPet(_user.Role, _user.TutorId, _user.ClinicId, pet.IdTutor, pet.IdClinica))
            return (false, "Pet fora do escopo de acesso.", null);
        var validation = CareRules.ValidatePet(pet.NmPet, pet.DsSexo, pet.DtNascimento, _time.GetUtcNow().UtcDateTime);
        if (validation != null) return (false, validation, null);
        if (pet.IdClinica.HasValue && !await _context.Repository<TutorClinic>().AnyAsync(l => l.TutorId == pet.IdTutor && l.ClinicId == pet.IdClinica))
            return (false, "Tutor não vinculado à clínica.", null);
        var tutorCount = await _context.Repository<Tutor>().CountAsync(_context.Repository<Tutor>().Query.Where(t => t.IdTutor == pet.IdTutor));

        if (tutorCount == 0)
            return (false, "O tutor informado não existe.", null);

        var speciesCount = await _context.Repository<Species>().CountAsync(_context.Repository<Species>().Query.Where(e => e.IdEspecie == pet.IdEspecie));

        if (speciesCount == 0)
            return (false, "A espécie informada não existe.", null);

        if (pet.IdRaca != null)
        {
            var breedCount = await _context.Repository<Breed>().CountAsync(_context.Repository<Breed>().Query.Where(r => r.IdRaca == pet.IdRaca && r.IdEspecie == pet.IdEspecie));

            if (breedCount == 0)
                return (false, "A raça informada não existe ou não pertence à espécie escolhida.", null);
        }

        if (pet.IdClinica != null)
        {
            var clinicCount = await _context.Repository<Clinic>().CountAsync(_context.Repository<Clinic>().Query.Where(c => c.IdClinica == pet.IdClinica));

            if (clinicCount == 0)
                return (false, "A clínica informada não existe.", null);
        }

        pet.IdPet = 0;
        _context.Repository<Pet>().Add(pet);
        await _context.SaveChangesAsync();

        return (true, null, pet);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, Pet pet)
    {
        using var activity = Telemetry.Activities.StartActivity("PetService.UpdateAsync");
        if (id != pet.IdPet)
            return (false, "O id da rota é diferente do id enviado no corpo da requisição.");

        var currentPet = await _context.Repository<Pet>().FirstAsync(p => p.IdPet == id);

        if (currentPet == null)
            return (false, "Pet não encontrado.");

        if (!CareRules.CanOwnPet(_user.Role, _user.TutorId, _user.ClinicId, pet.IdTutor, pet.IdClinica))
            return (false, "Pet fora do escopo de acesso.");
        var validation = CareRules.ValidatePet(pet.NmPet, pet.DsSexo, pet.DtNascimento, _time.GetUtcNow().UtcDateTime);
        if (validation != null) return (false, validation);
        if (pet.IdClinica.HasValue && !await _context.Repository<TutorClinic>().AnyAsync(l => l.TutorId == pet.IdTutor && l.ClinicId == pet.IdClinica))
            return (false, "Tutor não vinculado à clínica.");
        var tutorCount = await _context.Repository<Tutor>().CountAsync(_context.Repository<Tutor>().Query.Where(t => t.IdTutor == pet.IdTutor));

        if (tutorCount == 0)
            return (false, "O tutor informado não existe.");

        var speciesCount = await _context.Repository<Species>().CountAsync(_context.Repository<Species>().Query.Where(e => e.IdEspecie == pet.IdEspecie));

        if (speciesCount == 0)
            return (false, "A espécie informada não existe.");

        if (pet.IdRaca != null)
        {
            var breedCount = await _context.Repository<Breed>().CountAsync(_context.Repository<Breed>().Query.Where(r => r.IdRaca == pet.IdRaca && r.IdEspecie == pet.IdEspecie));

            if (breedCount == 0)
                return (false, "A raça informada não existe ou não pertence à espécie escolhida.");
        }

        if (pet.IdClinica != null)
        {
            var clinicCount = await _context.Repository<Clinic>().CountAsync(_context.Repository<Clinic>().Query.Where(c => c.IdClinica == pet.IdClinica));

            if (clinicCount == 0)
                return (false, "A clínica informada não existe.");
        }

        if (currentPet.IdTutor != pet.IdTutor)
            return (false, "Não é permitido transferir a titularidade do pet.");
        if (_user.Role == "TUTOR" && currentPet.IdClinica != pet.IdClinica)
            return (false, "A afiliação clínica deve ser gerenciada pela clínica.");
        currentPet.NmPet = pet.NmPet;
        currentPet.DtNascimento = pet.DtNascimento;
        currentPet.DsSexo = pet.DsSexo;
        currentPet.IdTutor = pet.IdTutor;
        currentPet.IdEspecie = pet.IdEspecie;
        currentPet.IdRaca = pet.IdRaca;
        currentPet.IdClinica = pet.IdClinica;

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        using var activity = Telemetry.Activities.StartActivity("PetService.DeleteAsync");
        var pet = await _context.Repository<Pet>().FirstAsync(p => p.IdPet == id);

        if (pet == null)
            return (false, "Pet não encontrado.");

        var reminderCount = await _context.Repository<Reminder>().CountAsync(_context.Repository<Reminder>().Query.Where(r => r.IdPet == id));

        if (reminderCount > 0)
            return (false, "Não é possível remover um pet que possui lembretes cadastrados.");

        _context.Repository<Pet>().Remove(pet);
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
