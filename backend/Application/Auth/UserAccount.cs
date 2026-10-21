namespace IRoute.Application.Auth;

public sealed record UserAccount(
    long Id,
    string Username,
    string PasswordHash,
    string Role,
    bool IsActive);
