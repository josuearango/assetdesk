using AssetDesk.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetDesk.Api.Infrastructure.Web;

/// <summary>
/// Traduce excepciones del dominio a respuestas HTTP con formato ProblemDetails (RFC 9457).
/// Asi los controllers no llevan try/catch: piden la operacion y, si el dominio se niega,
/// el error sale con el codigo correcto y un cuerpo consistente en toda la API.
/// </summary>
public class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var problem = exception switch
        {
            NotFoundException e => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Recurso no encontrado",
                Detail = e.Message
            },

            // 409 y no 400: la peticion estaba bien formada, lo que no lo permite es el
            // estado actual del dominio.
            DomainRuleException e => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Regla de negocio violada",
                Detail = e.Message
            },

            // Perdio la carrera contra otra escritura: el rowversion no coincide.
            DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflicto de concurrencia",
                Detail = "Alguien mas modifico este registro mientras lo editabas. Volve a cargarlo e intenta de nuevo."
            },

            // Cualquier otra cosa no es nuestra: que la maneje el pipeline por defecto y
            // salga como 500, sin filtrar detalles internos al cliente.
            _ => null
        };

        if (problem is null)
        {
            logger.LogError(exception, "Excepcion no controlada en {Path}", context.Request.Path);
            return false;
        }

        logger.LogInformation(
            "Peticion rechazada en {Path}: {Status} {Detail}",
            context.Request.Path, problem.Status, problem.Detail);

        problem.Instance = context.Request.Path;
        context.Response.StatusCode = problem.Status!.Value;
        await context.Response.WriteAsJsonAsync(problem, ct);

        return true;
    }
}
