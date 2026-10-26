using PetJourneyTutorApi.Queries;
using PetJourneyTutorApi.Hypermedia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Services;

namespace PetJourneyTutorApi.Controllers;

[ApiController]
[Route("api/lembretes")]
public class RemindersController : ControllerBase
{
    private readonly ReminderService _reminderService;

    public RemindersController(ReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    /// <summary>
    /// Lista todos os lembretes cadastrados.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Reminder>>> GetReminders([FromQuery] ReminderQuery options, CancellationToken ct)
    {
        var result = await _reminderService.SearchAsync(options, ct);
        PaginationHeaders.Apply(HttpContext, result);
        return Ok(result.Items);
    }

    /// <summary>
    /// Busca um lembrete pelo identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Reminder>> GetReminderById(int id)
    {
        var reminder = await _reminderService.GetByIdAsync(id);

        if (reminder == null)
            return NotFound("Lembrete não encontrado.");

        return Ok(reminder);
    }

    /// <summary>
    /// Cria um novo lembrete para um pet.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA,TUTOR")]
    [HttpPost]
    public async Task<ActionResult<Reminder>> CreateReminder(Reminder reminder)
    {
        var result = await _reminderService.CreateAsync(reminder);

        if (!result.Success)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetReminderById), new { id = result.Reminder!.IdLembrete }, result.Reminder);
    }

    /// <summary>
    /// Atualiza os dados de um lembrete.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA,TUTOR")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateReminder(int id, Reminder reminder)
    {
        var result = await _reminderService.UpdateAsync(id, reminder);

        if (!result.Success)
        {
            if (result.Error == "Lembrete não encontrado.")
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Remove um lembrete pelo identificador.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA,TUTOR")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteReminder(int id)
    {
        var result = await _reminderService.DeleteAsync(id);

        if (!result.Success)
            return NotFound(result.Error);

        return NoContent();
    }
}
