using PetJourneyTutorApi.Security;
using PetJourneyTutorApi.Domain;
using PetJourneyTutorApi.Observability;
using PetJourneyTutorApi.Persistence;

using PetJourneyTutorApi.Models;

namespace PetJourneyTutorApi.Services;

public partial class ReminderService
{
    private readonly IDataSession _context;

    private readonly ICurrentUser _user;
    private readonly TimeProvider _time;

    public ReminderService(IDataSession context, ICurrentUser user, TimeProvider time)
    {
        _context = context;
        _user = user;
        _time = time;
    }

    private IQueryable<Pet> AccessiblePets() => _user.Role switch
    {
        "ADMIN_SISTEMA" => _context.Repository<Pet>().Query,
        "TUTOR" when _user.TutorId.HasValue => _context.Repository<Pet>().Query.Where(p => p.IdTutor == _user.TutorId.Value),
        "ADMIN_CLINICA" or "VETERINARIO" when _user.ClinicId.HasValue => _context.Repository<Pet>().Query.Where(p => p.IdClinica == _user.ClinicId.Value),
        _ => _context.Repository<Pet>().Query.Where(p => false)
    };

    private IQueryable<Reminder> AccessibleReminders() => _context.Repository<Reminder>().Query
        .Where(r => AccessiblePets().Any(p => p.IdPet == r.IdPet));

    public async Task<List<Reminder>> GetAllAsync()
    {
        using var activity = Telemetry.Activities.StartActivity("ReminderService.GetAllAsync");
        return await _context.Repository<Reminder>().ListAsync(AccessibleReminders().OrderBy(r => r.DtLembrete));
    }

    public async Task<Reminder?> GetByIdAsync(int id)
    {
        using var activity = Telemetry.Activities.StartActivity("ReminderService.GetByIdAsync");
        return await _context.Repository<Reminder>().FirstAsync(r => r.IdLembrete == id);
    }

    public async Task<(bool Success, string? Error, Reminder? Reminder)> CreateAsync(Reminder reminder)
    {
        using var activity = Telemetry.Activities.StartActivity("ReminderService.CreateAsync");
        var petCount = await _context.Repository<Pet>().CountAsync(AccessiblePets().Where(p => p.IdPet == reminder.IdPet));

        if (petCount == 0)
            return (false, "O pet informado não existe.", null);

        if (reminder.DtLembrete.Date < _time.GetUtcNow().UtcDateTime.Date)
            return (false, "A data do lembrete não pode ser anterior à data atual.", null);

        if (string.IsNullOrWhiteSpace(reminder.DsStatus))
            reminder.DsStatus = "PENDENTE";

        var validation = CareRules.ValidateReminder(reminder.DtLembrete, reminder.DsStatus, _time.GetUtcNow().UtcDateTime);
        if (validation != null) return (false, validation, null);
        reminder.IdLembrete = 0;
        reminder.DtNotificado = null;
        _context.Repository<Reminder>().Add(reminder);
        await _context.SaveChangesAsync();

        return (true, null, reminder);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, Reminder reminder)
    {
        using var activity = Telemetry.Activities.StartActivity("ReminderService.UpdateAsync");
        if (id != reminder.IdLembrete)
            return (false, "O id da rota é diferente do id enviado no corpo da requisição.");

        var currentReminder = await _context.Repository<Reminder>().FirstAsync(r => r.IdLembrete == id);

        if (currentReminder == null)
            return (false, "Lembrete não encontrado.");

        var petCount = await _context.Repository<Pet>().CountAsync(AccessiblePets().Where(p => p.IdPet == reminder.IdPet));

        if (petCount == 0)
            return (false, "O pet informado não existe.");

        var validation = CareRules.ValidateReminder(reminder.DtLembrete, reminder.DsStatus, _time.GetUtcNow().UtcDateTime);
        if (validation != null) return (false, validation);
        currentReminder.IdPet = reminder.IdPet;
        currentReminder.DsTipo = reminder.DsTipo;
        currentReminder.DsDescricao = reminder.DsDescricao;
        currentReminder.DtLembrete = reminder.DtLembrete;

        currentReminder.DsStatus = reminder.DsStatus;

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        using var activity = Telemetry.Activities.StartActivity("ReminderService.DeleteAsync");
        var reminder = await _context.Repository<Reminder>().FirstAsync(r => r.IdLembrete == id);

        if (reminder == null)
            return (false, "Lembrete não encontrado.");

        _context.Repository<Reminder>().Remove(reminder);
        await _context.SaveChangesAsync();

        return (true, null);
    }
}
