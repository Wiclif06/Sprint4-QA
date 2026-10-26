using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using PetJourneyTutorApi.Queries;
namespace PetJourneyTutorApi.Hypermedia;
public static class PaginationHeaders
{
    public static void Apply<T>(HttpContext context, PageResult<T> page)
    {
        context.Response.Headers["X-Total-Count"] = page.Total.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers["X-Page"] = page.Page.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers["X-Page-Size"] = page.PageSize.ToString(CultureInfo.InvariantCulture);
        string Link(int number, string rel)
        {
            var values = context.Request.Query.ToDictionary(q => q.Key, q => (string?)q.Value.ToString(), StringComparer.OrdinalIgnoreCase);
            values["page"] = number.ToString(CultureInfo.InvariantCulture);
            values["pageSize"] = page.PageSize.ToString(CultureInfo.InvariantCulture);
            return $"<{QueryHelpers.AddQueryString(context.Request.PathBase + context.Request.Path, values)}>; rel=\"{rel}\"";
        }
        var links = new List<string> { Link(page.Page, "self"), Link(1, "first") };
        if ((long)page.Page * page.PageSize < page.Total) links.Add(Link(page.Page + 1, "next"));
        if (page.Page > 1) links.Add(Link(page.Page - 1, "prev"));
        var last = Math.Max(1, (int)(((long)page.Total + page.PageSize - 1) / page.PageSize));
        links.Add(Link(last, "last"));
        context.Response.Headers["Link"] = string.Join(", ", links);
    }
}
