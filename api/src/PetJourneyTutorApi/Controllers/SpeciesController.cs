using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Services;
using PetJourneyTutorApi.Queries;
using PetJourneyTutorApi.Hypermedia;
namespace PetJourneyTutorApi.Controllers;
[ApiController, Route("api/especies")]
public sealed class SpeciesController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CatalogQuery options, CancellationToken ct)
    {
        var result = await catalog.SpeciesAsync(options, ct);
        PaginationHeaders.Apply(HttpContext, result);
        return Ok(result.Items);
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
        await catalog.SpeciesByIdAsync(id, ct) is { } item ? Ok(item) : NotFound();
    [HttpGet("{id:int}/racas")]
    public async Task<IActionResult> GetBreedsBySpecies(int id, [FromQuery] CatalogQuery options, CancellationToken ct)
    {
        if (await catalog.SpeciesByIdAsync(id, ct) is null) return NotFound();
        options.SpeciesId = id;
        var result = await catalog.BreedsAsync(options, ct);
        PaginationHeaders.Apply(HttpContext, result);
        return Ok(result.Items);
    }
}
