using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetJourneyTutorApi.Errors;
namespace PetJourneyTutorApi;
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            RequestValidationException => 400, AccessDeniedException => 403,
            DataConflictException => 409, DependencyUnavailableException => 503, _ => 500
        };
        var correlation = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        if (status >= 500) logger.LogError(exception, "Falha HTTP {StatusCode}. CorrelationId: {CorrelationId}", status, correlation);
        else logger.LogWarning(exception, "Requisição rejeitada HTTP {StatusCode}. CorrelationId: {CorrelationId}", status, correlation);
        var title = status switch
        {
            400 => "Parâmetros inválidos", 403 => "Acesso negado", 409 => "Conflito de dados", 503 => "Dependência indisponível", _ => "Erro interno"
        };
        var problem = new ProblemDetails { Status = status, Title = title, Instance = context.Request.Path,
            Detail = status == 400 ? exception.Message : "Não foi possível concluir a operação." };
        problem.Extensions["correlationId"] = correlation;
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: ct);
        return true;
    }
}
