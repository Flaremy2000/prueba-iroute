using IRoute.Application.Abstractions;
using IRoute.Application.Auth;
using IRoute.Domain.Exceptions;

namespace IRoute.Tests;

public sealed class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeTokenService _tokens = new();

    [Fact]
    public async Task LoginAsync_WithoutCredentials_Throws()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.LoginAsync(
                new LoginCommand(" ", " "),
                CancellationToken.None));

        Assert.Contains("obligatorios", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_ThrowsGenericError()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.LoginAsync(
                new LoginCommand("admin", "Admin123!"),
                CancellationToken.None));

        Assert.Equal("Usuario o contraseña inválidos.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsGenericError()
    {
        _users.User = new UserAccount(1, "admin", "hash", "Admin", true);
        _hasher.IsValid = false;
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.LoginAsync(
                new LoginCommand("admin", "incorrecta"),
                CancellationToken.None));

        Assert.Equal("Usuario o contraseña inválidos.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsGenericError()
    {
        _users.User = new UserAccount(1, "admin", "hash", "Admin", false);
        _hasher.IsValid = true;
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<DomainValidationException>(
            () => service.LoginAsync(
                new LoginCommand("admin", "Admin123!"),
                CancellationToken.None));

        Assert.Equal("Usuario o contraseña inválidos.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        _users.User = new UserAccount(1, "admin", "hash", "Admin", true);
        _hasher.IsValid = true;
        var service = CreateService();

        var result = await service.LoginAsync(
            new LoginCommand(" admin ", "Admin123!"),
            CancellationToken.None);

        Assert.Equal("token", result.AccessToken);
        Assert.Equal("admin", result.Username);
        Assert.Equal("Admin", result.Role);
    }

    private AuthService CreateService() => new(_users, _hasher, _tokens);

    private sealed class FakeUserRepository : IUserRepository
    {
        public UserAccount? User { get; set; }

        public Task<UserAccount?> FindByUsernameAsync(
            string username,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                string.Equals(User?.Username, username, StringComparison.Ordinal)
                    ? User
                    : null);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public bool IsValid { get; set; }

        public string Hash(string password) => "hash";

        public bool Verify(string password, string passwordHash) => IsValid;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public TokenResult CreateToken(UserAccount user) =>
            new("token", DateTimeOffset.UtcNow.AddMinutes(5));
    }
}
