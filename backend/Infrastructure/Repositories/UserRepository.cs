using IRoute.Application.Abstractions;
using IRoute.Application.Auth;
using IRoute.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IRoute.Infrastructure.Repositories;

public sealed class UserRepository(CommerceDbContext dbContext) : IUserRepository
{
    public async Task<UserAccount?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Username == username)
            .Select(user => new UserAccount(
                user.Id,
                user.Username,
                user.PasswordHash,
                user.Role,
                user.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
