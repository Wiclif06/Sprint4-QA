using PetJourneyTutorApi.Queries;
using PetJourneyTutorApi.Hypermedia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Services;

namespace PetJourneyTutorApi.Controllers;

[ApiController]
[Route("api/clinicas")]
public class ClinicsController : ControllerBase
{
    private readonly ClinicService _clinicService;

    public ClinicsController(ClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    /// <summary>
    /// Lista todas as clínicas.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Clinic>>> GetClinics([FromQuery] CatalogQuery options, CancellationToken ct)
    {
        var result = await _clinicService.SearchAsync(options, ct);
        PaginationHeaders.Apply(HttpContext, result);
        return Ok(result.Items);
    }

    /// <summary>
    /// Busca uma clínica pelo identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Clinic>> GetClinicById(int id)
    {
        var clinic = await _clinicService.GetByIdAsync(id);

        if (clinic == null)
            return NotFound("Clínica não encontrada.");

        return Ok(clinic);
    }
}
