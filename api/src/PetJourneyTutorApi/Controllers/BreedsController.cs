using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Services;
using PetJourneyTutorApi.Queries;
using PetJourneyTutorApi.Hypermedia;
namespace PetJourneyTutorApi.Controllers;
[ApiController, Route("api/racas")]
public sealed class BreedsController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CatalogQuery options, CancellationToken ct)
    {
        var result = await catalog.BreedsAsync(options, ct);
        PaginationHeaders.Apply(HttpContext, result);
        return Ok(result.Items);
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct) =>
        await catalog.BreedByIdAsync(id, ct) is { } item ? Ok(item) : NotFound();
}
