using IRoute.Application.Auth;

namespace IRoute.Application.Abstractions;

public interface IUserRepository
{
    Task<UserAccount?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken);
}
