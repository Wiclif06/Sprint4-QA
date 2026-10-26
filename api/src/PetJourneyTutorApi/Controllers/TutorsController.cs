using PetJourneyTutorApi.Queries;
using PetJourneyTutorApi.Hypermedia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Services;

namespace PetJourneyTutorApi.Controllers;

[ApiController]
[Route("api/tutores")]
public class TutorsController : ControllerBase
{
    private readonly TutorService _tutorService;

    public TutorsController(TutorService tutorService)
    {
        _tutorService = tutorService;
    }

    /// <summary>
    /// Lista todos os tutores cadastrados.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tutor>>> GetTutors([FromQuery] TutorQuery options, CancellationToken ct)
    {
        var result = await _tutorService.SearchAsync(options, ct);
        PaginationHeaders.Apply(HttpContext, result);
        return Ok(result.Items);
    }

    /// <summary>
    /// Busca um tutor pelo identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Tutor>> GetTutorById(int id)
    {
        var tutor = await _tutorService.GetByIdAsync(id);

        if (tutor == null)
            return NotFound("Tutor não encontrado.");

        return Ok(tutor);
    }

    /// <summary>
    /// Lista os pets vinculados a um tutor.
    /// </summary>
    [HttpGet("{id:int}/pets")]
    public async Task<ActionResult<IEnumerable<Pet>>> GetTutorPets(int id)
    {
        var pets = await _tutorService.GetPetsByTutorAsync(id);

        if (pets == null)
            return NotFound("Tutor não encontrado.");

        return Ok(pets);
    }


    /// <summary>
    /// Lista as clínicas vinculadas ao tutor dentro do escopo de acesso.
    /// </summary>
    [HttpGet("{id:int}/clinicas")]
    public async Task<ActionResult<IEnumerable<Clinic>>> GetTutorClinics(int id)
    {
        var clinics = await _tutorService.GetTutorClinicsAsync(id);

        if (clinics == null)
            return NotFound("Tutor não encontrado.");

        return Ok(clinics);
    }

    /// <summary>
    /// Cria um novo tutor.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA")]
    [HttpPost]
    public async Task<ActionResult<Tutor>> CreateTutor(Tutor tutor)
    {
        var result = await _tutorService.CreateAsync(tutor);

        if (!result.Success)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetTutorById), new { id = result.Tutor!.IdTutor }, result.Tutor);
    }

    /// <summary>
    /// Atualiza os dados de um tutor.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA,TUTOR")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTutor(int id, Tutor tutor)
    {
        var result = await _tutorService.UpdateAsync(id, tutor);

        if (!result.Success)
        {
            if (result.Error == "Tutor não encontrado.")
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Vincula o tutor à clínica e afilia seus pets sem clínica ou já vinculados a ela.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA")]
    [HttpPut("{tutorId:int}/clinica/{clinicaId:int}")]
    public async Task<IActionResult> LinkTutorToClinic(int tutorId, int clinicaId)
    {
        var result = await _tutorService.LinkToClinicAsync(tutorId, clinicaId);

        if (!result.Success)
            return NotFound(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Remove a afiliação clínica dos pets de um tutor.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA")]
    [HttpDelete("{tutorId:int}/clinica")]
    public async Task<IActionResult> UnlinkTutorFromClinic(int tutorId)
    {
        var result = await _tutorService.UnlinkFromClinicAsync(tutorId);

        if (!result.Success)
            return NotFound(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Remove um tutor.
    /// </summary>
    [Authorize(Roles = "ADMIN_SISTEMA")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTutor(int id)
    {
        var result = await _tutorService.DeleteAsync(id);

        if (!result.Success)
        {
            if (result.Error == "Tutor não encontrado.")
                return NotFound(result.Error);

            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
