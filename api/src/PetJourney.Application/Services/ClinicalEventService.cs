using PetJourneyTutorApi.Mongo;
using PetJourneyTutorApi.Queries;
using PetJourneyTutorApi.Security;
using PetJourneyTutorApi.Errors;
namespace PetJourneyTutorApi.Services;
public sealed class ClinicalEventService(PetService pets, IClinicalEventRepository events, ICurrentUser user, TimeProvider time)
{
    public async Task<PageResult<ClinicalEvent>?> SearchAsync(int petId, ClinicalEventQuery options, CancellationToken ct)
    {
        Paging.Validate(options);
        if (await pets.GetByIdAsync(petId) is null) return null;
        return await events.SearchAsync(petId, options, ct);
    }
    public async Task<ClinicalEvent?> GetAsync(int petId, string id, CancellationToken ct)
    {
        if (await pets.GetByIdAsync(petId) is null) return null;
        return await events.GetAsync(petId, id, ct);
    }
    public async Task<ClinicalEvent?> CreateAsync(int petId, string type, string description, CancellationToken ct)
    {
        if (user.Role is not ("ADMIN_SISTEMA" or "ADMIN_CLINICA" or "VETERINARIO"))
            throw new AccessDeniedException();
        if (await pets.GetByIdAsync(petId) is null) return null;
        type = type.Trim(); description = description.Trim();
        if (type.Length is < 2 or > 60 || description.Length is < 2 or > 1000)
            throw new RequestValidationException("Tipo deve ter de 2 a 60 caracteres e descrição de 2 a 1000 caracteres após remover espaços nas extremidades.");
        if (string.IsNullOrWhiteSpace(user.Username)) throw new AccessDeniedException();
        return await events.AddAsync(new ClinicalEvent { PetId = petId, Type = type, Description = description,
            CreatedAtUtc = time.GetUtcNow().UtcDateTime, CreatedBy = user.Username }, ct);
    }
}
