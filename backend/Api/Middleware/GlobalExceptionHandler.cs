using IRoute.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IRoute.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var isValidationError = exception is DomainValidationException;
        var statusCode = isValidationError
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        if (isValidationError)
        {
            logger.LogWarning(
                "Solicitud inválida en {Path}: {Message}",
                httpContext.Request.Path,
                exception.Message);
        }
        else
        {
            logger.LogError(
                exception,
                "Error no controlado en {Path}",
                httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = isValidationError
                ? "Solicitud inválida"
                : "Error interno del servidor",
            Detail = isValidationError
                ? exception.Message
                : "Ocurrió un error inesperado. Revisa los logs del servidor.",
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
