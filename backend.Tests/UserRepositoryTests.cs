using IRoute.Infrastructure.Persistence;
using IRoute.Infrastructure.Repositories;
using IRoute.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace IRoute.Tests;

public sealed class UserRepositoryTests
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("IROUTE_TEST_DB")
        ?? @"Server=.\SQLEXPRESS;Database=IRouteCommerce;Trusted_Connection=True;TrustServerCertificate=True;";

    private static bool DbTestsEnabled =>
        Environment.GetEnvironmentVariable("IROUTE_DB_TESTS") == "1";

    [Fact]
    public async Task FindByUsername_ReturnsSeededAdminWithVerifiablePassword()
    {
        if (!DbTestsEnabled)
        {
            return;
        }

        await using var db = CreateContext();
        var repository = new UserRepository(db);

        var user = await repository.FindByUsernameAsync("admin", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("admin", user.Username);
        Assert.Equal("Admin", user.Role);
        Assert.True(user.IsActive);
        Assert.True(new Pbkdf2PasswordHasher().Verify("Admin123!", user.PasswordHash));
    }

    [Fact]
    public async Task FindByUsername_UnknownUser_ReturnsNull()
    {
        if (!DbTestsEnabled)
        {
            return;
        }

        await using var db = CreateContext();
        var repository = new UserRepository(db);

        var user = await repository.FindByUsernameAsync(
            $"missing-{Guid.NewGuid():N}",
            CancellationToken.None);

        Assert.Null(user);
    }

    private static CommerceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CommerceDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new CommerceDbContext(options);
    }
}
