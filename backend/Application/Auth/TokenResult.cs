namespace IRoute.Application.Auth;

public sealed record TokenResult(string AccessToken, DateTimeOffset ExpiresAt);
