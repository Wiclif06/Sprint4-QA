using System.Collections;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Mongo;
namespace PetJourneyTutorApi.Hypermedia;
public sealed class ResourceLinksFilter(IOptions<JsonOptions> json) : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { Value: { } value } result) return;
        if (result.StatusCode >= 400)
        {
            if (value is string message)
            {
                var problem = new ProblemDetails { Status = result.StatusCode, Title = message, Instance = context.HttpContext.Request.Path };
                problem.Extensions["correlationId"] = context.HttpContext.Items["CorrelationId"]?.ToString() ?? context.HttpContext.TraceIdentifier;
                result.Value = problem;
                result.ContentTypes.Clear(); result.ContentTypes.Add("application/problem+json");
            }
            return;
        }
        object Enrich(object item)
        {
            var path = item switch
            {
                Pet x => $"/api/pets/{x.IdPet}", Reminder x => $"/api/lembretes/{x.IdLembrete}",
                Tutor x => $"/api/tutores/{x.IdTutor}", Clinic x => $"/api/clinicas/{x.IdClinica}",
                Species x => $"/api/especies/{x.IdEspecie}", Breed x => $"/api/racas/{x.IdRaca}",
                ClinicalEvent x when x.Id != null => $"/api/pets/{x.PetId}/eventos/{x.Id}", _ => null
            };
            if (path is null) return item;
            path = context.HttpContext.Request.PathBase + path;
            var node = JsonSerializer.SerializeToNode(item, item.GetType(), json.Value.JsonSerializerOptions)!.AsObject();
            var links = new JsonObject { ["self"] = new JsonObject { ["href"] = path, ["method"] = "GET" } };
            void Link(string rel, string url, string method = "GET") => links[rel] = new JsonObject { ["href"] = url, ["method"] = method };
            if (item is Pet) { Link("reminders", path + "/lembretes"); Link("events", path + "/eventos"); }
            if (item is Tutor) { Link("pets", path + "/pets"); Link("clinics", path + "/clinicas"); }
            if (item is Species) Link("breeds", path + "/racas");
            var user = context.HttpContext.User;
            var canManage = user.IsInRole("ADMIN_SISTEMA") || user.IsInRole("ADMIN_CLINICA") || user.IsInRole("TUTOR");
            if (canManage && item is Pet or Reminder or Tutor) Link("update", path, "PUT");
            if ((canManage && item is Pet or Reminder) || (user.IsInRole("ADMIN_SISTEMA") && item is Tutor)) Link("delete", path, "DELETE");
            node["_links"] = links;
            return node;
        }
        result.Value = value is IEnumerable items and not string and not JsonNode ? items.Cast<object>().Select(Enrich).ToArray() : Enrich(value);
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
}
