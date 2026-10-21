using IRoute.Infrastructure.Security;

namespace IRoute.Tests;

public sealed class Pbkdf2PasswordHasherTests
{
    private const string SeedHash =
        "PBKDF2-SHA256$210000$3P3bD+W5i0Qz6OyNuwPC2Q==$HBX4vAeJWV01LD2PKkUvWD8bvARKrk7CIv0dh6zpNA0=";

    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_SeedHash_AcceptsAdminPassword()
    {
        Assert.True(_hasher.Verify("Admin123!", SeedHash));
    }

    [Fact]
    public void HashAndVerify_ValidPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Clave-Segura-123!");

        Assert.StartsWith("PBKDF2-SHA256$210000$", hash);
        Assert.True(_hasher.Verify("Clave-Segura-123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Clave-Segura-123!");

        Assert.False(_hasher.Verify("Otra-Clave", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto")]
    [InlineData("PBKDF2-SHA256$abc$c2FsdA==$aGFzaA==")]
    [InlineData("PBKDF2-SHA256$100$c2FsdA==$aGFzaA==")]
    [InlineData("PBKDF2-SHA256$210000$no-base64$aGFzaA==")]
    [InlineData("PBKDF2-SHA256$210000$c2FsdA==$no-base64")]
    [InlineData("PBKDF2-SHA256$210000$c2FsdA==$")]
    public void Verify_MalformedHash_ReturnsFalse(string hash)
    {
        Assert.False(_hasher.Verify("Admin123!", hash));
    }

    [Fact]
    public void Hash_EmptyPassword_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() => _hasher.Hash(" "));
    }
}
