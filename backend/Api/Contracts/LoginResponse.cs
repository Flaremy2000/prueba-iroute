namespace IRoute.Api.Contracts;

/// <summary>
/// Token de acceso emitido al iniciar sesión.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    string Username,
    string Role);
