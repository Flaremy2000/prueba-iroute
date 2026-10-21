namespace IRoute.Application.Auth;

public sealed record LoginResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string Username,
    string Role);
