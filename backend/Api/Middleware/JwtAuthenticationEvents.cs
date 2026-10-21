using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;

namespace IRoute.Api.Middleware;

public sealed class JwtAuthenticationEvents : JwtBearerEvents
{
    public JwtAuthenticationEvents()
    {
        OnChallenge = HandleChallengeAsync;
        OnForbidden = HandleForbiddenAsync;
    }

    private static async Task HandleChallengeAsync(JwtBearerChallengeContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.HandleResponse();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "No autorizado",
            Detail = "Debes iniciar sesión para acceder a este recurso.",
            Instance = context.Request.Path
        });
    }

    private static async Task HandleForbiddenAsync(ForbiddenContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Acceso denegado",
            Detail = "No tienes permisos para acceder a este recurso.",
            Instance = context.Request.Path
        });
    }
}
