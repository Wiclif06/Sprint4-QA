using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Hypermedia;
using PetJourneyTutorApi.Mongo;
using PetJourneyTutorApi.Services;
namespace PetJourneyTutorApi.Controllers;
[ApiController, Route("api/pets/{petId:int}/eventos")]
public sealed class ClinicalEventsController(ClinicalEventService events) : ControllerBase
{
    public sealed record CreateClinicalEvent([Required, StringLength(60, MinimumLength = 2)] string Type,
        [Required, StringLength(1000, MinimumLength = 2)] string Description);
    [HttpGet]
    public async Task<IActionResult> Get(int petId, [FromQuery] ClinicalEventQuery options, CancellationToken ct)
    {
        var result = await events.SearchAsync(petId, options, ct);
        if (result is null) return NotFound();
        PaginationHeaders.Apply(HttpContext, result);
        return Ok(result.Items);
    }
    [HttpGet("{eventId}")]
    public async Task<IActionResult> GetById(int petId, [RegularExpression("^[a-fA-F0-9]{24}$")] string eventId, CancellationToken ct) =>
        await events.GetAsync(petId, eventId, ct) is { } item ? Ok(item) : NotFound();
    [HttpPost, Authorize(Roles = "ADMIN_SISTEMA,ADMIN_CLINICA,VETERINARIO")]
    public async Task<IActionResult> Post(int petId, CreateClinicalEvent input, CancellationToken ct)
    {
        var item = await events.CreateAsync(petId, input.Type, input.Description, ct);
        return item is null ? NotFound() : CreatedAtAction(nameof(GetById), new { petId, eventId = item.Id }, item);
    }
}
