using System.ComponentModel.DataAnnotations;
using PetJourneyTutorApi.Persistence;
using PetJourneyTutorApi.Errors;
namespace PetJourneyTutorApi.Queries;
public class PageQuery : IValidatableObject
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [MaxLength(20)] public string SortBy { get; set; } = "id";
    public bool Descending { get; set; }
    protected virtual string[] AllowedSorts => ["id", "name"];
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!AllowedSorts.Contains(SortBy))
            yield return new ValidationResult($"sortBy deve ser: {string.Join(", ", AllowedSorts)}.", [nameof(SortBy)]);
    }
}
public class NamedQuery : PageQuery
{
    [MaxLength(200)] public string? Name { get; set; }
}
public sealed class PetQuery : NamedQuery
{
    [Range(1, int.MaxValue)] public int? TutorId { get; set; }
    [Range(1, int.MaxValue)] public int? ClinicId { get; set; }
    [Range(1, int.MaxValue)] public int? SpeciesId { get; set; }
    [Range(1, int.MaxValue)] public int? BreedId { get; set; }
    [RegularExpression("^[MF]$")] public string? Sex { get; set; }
    protected override string[] AllowedSorts => ["id", "name", "birthDate"];
}
public sealed class ReminderQuery : PageQuery
{
    public ReminderQuery() => SortBy = "date";
    [Range(1, int.MaxValue)] public int? PetId { get; set; }
    [RegularExpression("^(PENDENTE|CONCLUIDO|CANCELADO)$")] public string? Status { get; set; }
    [MaxLength(100)] public string? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    protected override string[] AllowedSorts => ["id", "date", "type", "status"];
    public override IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (var result in base.Validate(context)) yield return result;
        if (From > To) yield return new ValidationResult("from deve ser anterior ou igual a to.", [nameof(From), nameof(To)]);
    }
}
public sealed class TutorQuery : NamedQuery
{
    [MaxLength(150)] public string? Email { get; set; }
    [MaxLength(20)] public string? Plan { get; set; }
    protected override string[] AllowedSorts => ["id", "name", "email"];
}
public sealed class CatalogQuery : NamedQuery
{
    [Range(1, int.MaxValue)] public int? SpeciesId { get; set; }
    [MaxLength(20)] public string? Status { get; set; }
}
public sealed record PageResult<T>(List<T> Items, int Total, int Page, int PageSize);
public static class Paging
{
    public static void Validate(PageQuery options)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(options, new ValidationContext(options), errors, true))
            throw new RequestValidationException(string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    public static async Task<PageResult<T>> ReadAsync<T>(IRepository<T> repository, IOrderedQueryable<T> query, PageQuery options, CancellationToken ct = default) where T : class
    {
        Validate(options);
        var total = await repository.CountAsync(query, ct);
        var offset = ((long)options.Page - 1) * options.PageSize;
        // A distant page is empty. Cast only after proving that the offset fits the count (int).
        var items = offset >= total ? [] : await repository.ListAsync(query.Skip((int)offset).Take(options.PageSize), ct);
        return new PageResult<T>(items, total, options.Page, options.PageSize);
    }
}
