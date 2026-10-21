using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IRoute.Application.Auth;
using IRoute.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace IRoute.Tests;

public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "IRoute.Test",
        Audience = "IRoute.Test.Client",
        Key = "IRoute-Test-Key-With-At-Least-32-Bytes-2026",
        ExpirationMinutes = 15
    };

    [Fact]
    public void CreateToken_ReturnsSignedTokenWithUserClaims()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options));
        var user = new UserAccount(7, "admin", "hash", "Admin", true);

        var result = service.CreateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal(Options.Issuer, token.Issuer);
        Assert.Contains(Options.Audience, token.Audiences);
        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == "7");
        Assert.Contains(token.Claims, claim =>
            claim.Type == ClaimTypes.Name && claim.Value == "admin");
        Assert.Contains(token.Claims, claim =>
            claim.Type == ClaimTypes.Role && claim.Value == "Admin");
        Assert.Contains(token.Claims, claim =>
            claim.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrWhiteSpace(claim.Value));
    }

    [Fact]
    public void CreateToken_UsesConfiguredExpiration()
    {
        var options = new JwtOptions
        {
            Issuer = Options.Issuer,
            Audience = Options.Audience,
            Key = Options.Key,
            ExpirationMinutes = 5
        };
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(options));

        var result = service.CreateToken(new UserAccount(1, "user", "hash", "User", true));

        Assert.InRange(
            result.ExpiresAt,
            DateTimeOffset.UtcNow.AddMinutes(4),
            DateTimeOffset.UtcNow.AddMinutes(6));
    }
}
