using System.Security.Claims;
using IRoute.Api.Contracts;
using IRoute.Application.Abstractions;
using IRoute.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IRoute.Api.Controllers;

/// <summary>
/// Autenticación de usuarios mediante JWT.
/// </summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// Inicia sesión y devuelve un token JWT.
    /// </summary>
    /// <param name="request">Usuario y contraseña.</param>
    /// <response code="200">Credenciales válidas.</response>
    /// <response code="400">Faltan credenciales o son inválidas.</response>
    /// <response code="429">Demasiados intentos de inicio de sesión.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            new LoginCommand(request.Username, request.Password),
            cancellationToken);

        return Ok(new LoginResponse(
            result.AccessToken,
            "Bearer",
            result.ExpiresAt,
            result.Username,
            result.Role));
    }

    /// <summary>
    /// Devuelve el usuario asociado al token actual.
    /// </summary>
    /// <response code="200">Token válido.</response>
    /// <response code="401">Token ausente o inválido.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var username = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";

        return Ok(new CurrentUserResponse(username, role));
    }
}
