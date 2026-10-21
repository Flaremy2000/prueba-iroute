using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IRoute.Api.Contracts;
using IRoute.Application.Abstractions;
using IRoute.Application.Auth;
using IRoute.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IRoute.Tests;

public sealed class AuthControllerTests : IDisposable
{
    private readonly AuthApiFactory _factory = new();

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "Admin123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Equal("Bearer", login.TokenType);
        Assert.Equal("admin", login.Username);
        Assert.Equal("Admin", login.Role);
        Assert.True(login.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns400WithGenericMessage()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "incorrecta" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Usuario o contraseña inválidos.", body);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns400WithGenericMessage()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "nadie", password = "Admin123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Usuario o contraseña inválidos.", body);
    }

    [Fact]
    public async Task Login_InactiveUser_Returns400()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();
        factory.Users.User = factory.Users.User! with { IsActive = false };

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "Admin123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithoutPassword_Returns400()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        using var client = _factory.CreateClient();
        var login = await LoginAsync(client);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(user);
        Assert.Equal("admin", user.Username);
        Assert.Equal("Admin", user.Role);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No autorizado", body);
    }

    [Fact]
    public async Task Login_AboveRateLimit_Returns429()
    {
        using var factory = new AuthApiFactory();
        using var client = factory.CreateClient();

        HttpResponseMessage? lastResponse = null;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            lastResponse?.Dispose();
            lastResponse = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { username = "admin", password = "incorrecta" });
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        lastResponse.Dispose();
    }

    public void Dispose() => _factory.Dispose();

    private static async Task<LoginResponse> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { username = "admin", password = "Admin123!" });

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        public FakeUserRepository Users { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IUserRepository>();
                services.AddSingleton<IUserRepository>(Users);

                Users.User = new UserAccount(
                    1,
                    "admin",
                    new Pbkdf2PasswordHasher().Hash("Admin123!"),
                    "Admin",
                    true);
            });
        }
    }

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
}
