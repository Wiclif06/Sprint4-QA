using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Mongo;
namespace PetJourneyTutorApi.Hypermedia;
public sealed class ResourceLinksSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(Pet) && context.Type != typeof(Reminder) && context.Type != typeof(Tutor) &&
            context.Type != typeof(Clinic) && context.Type != typeof(Species) && context.Type != typeof(Breed) && context.Type != typeof(ClinicalEvent)) return;
        schema.Properties["_links"] = new OpenApiSchema
        {
            Type = "object", ReadOnly = true,
            Description = "Links disponíveis ao perfil autenticado. href pode ser relativo à origem da requisição.",
            AdditionalProperties = new OpenApiSchema { Type = "object", Properties = new Dictionary<string, OpenApiSchema>
            {
                ["href"] = new() { Type = "string" }, ["method"] = new() { Type = "string" }
            }}
        };
    }
}
