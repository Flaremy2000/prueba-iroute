namespace IRoute.Api.Contracts;

/// <summary>
/// Usuario autenticado a partir del token actual.
/// </summary>
public sealed record CurrentUserResponse(string Username, string Role);
