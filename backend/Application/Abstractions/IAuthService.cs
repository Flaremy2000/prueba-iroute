using IRoute.Application.Auth;

namespace IRoute.Application.Abstractions;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken);
}
