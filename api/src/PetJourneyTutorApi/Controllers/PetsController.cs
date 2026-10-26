using PetJourneyTutorApi.Queries;
using PetJourneyTutorApi.Hypermedia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Services;

namespace PetJourneyTutorApi.Controllers;

[ApiController]
[Route("api/pets")]
public class PetsController : ControllerBase
{
    private readonly PetService _petService;

    public PetsController(PetService petService)
    {
        _petService = petService;
    }

    /// <summary>
    /// Lista todos os pets cadastrados.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pet>>> GetPets([FromQuery] PetQuery options, CancellationToken ct)
    {
        var result = await _petService.SearchAsync(options, ct);
        PaginationHeaders.Apply(HttpContext, result);
        return Ok(result.Items);
    }

    /// <summary>
    /// Busca um pet pelo identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Pet>> GetPetById(int id)
    {
        var pet = await _petService.GetByIdAsync(id);

        if (pet == null)
            return NotFound("Pet não encontrado.");

        return Ok(pet);
    }

    /// <summary>
    /// Lista os lembretes vinculados a um pet.
    /// </summary>
    [HttpGet("{petId:int}/lembretes")]
    public async Task<ActionResult<IEnumerable<Reminder>>> GetPetReminders(int petId)
    {
        var reminders = await _petService.GetRemindersByPetAsync(petId);

        if (reminders == null)
            return NotFound("Pet não encontrado.");

        return Ok(reminders);
    }

    /// <summary>
    /// Consulta a timeline de lembretes do pet no mês e ano informados.
    /// </summary>
    [HttpGet("{petId:int}/timeline")]
    public async Task<ActionResult<IEnumerable<TimelineItem>>> GetPetTimeline(int petId, [FromQuery] int mes, [FromQuery] int ano)
    {
        var result = await _petService.GetTimelineAsync(petId, mes, ano);

        if (!result.Success)
        {
            if (result.Error == "Pet não encontrado.")
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return Ok(result.Timeline);
    }

    /// <summary>
    /// Cria um novo pet vinculado a um tutor.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA,TUTOR")]
    [HttpPost]
    public async Task<ActionResult<Pet>> CreatePet(Pet pet)
    {
        var result = await _petService.CreateAsync(pet);

        if (!result.Success)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetPetById), new { id = result.Pet!.IdPet }, result.Pet);
    }

    /// <summary>
    /// Atualiza os dados de um pet.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA,TUTOR")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePet(int id, Pet pet)
    {
        var result = await _petService.UpdateAsync(id, pet);

        if (!result.Success)
        {
            if (result.Error == "Pet não encontrado.")
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Remove um pet.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA,TUTOR")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePet(int id)
    {
        var result = await _petService.DeleteAsync(id);

        if (!result.Success)
        {
            if (result.Error == "Pet não encontrado.")
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
