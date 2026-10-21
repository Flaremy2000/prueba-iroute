using IRoute.Application.Abstractions;
using IRoute.Domain.Exceptions;

namespace IRoute.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    private const string InvalidCredentialsMessage = "Usuario o contraseña inválidos.";

    public async Task<LoginResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Username) ||
            string.IsNullOrWhiteSpace(command.Password))
        {
            throw new DomainValidationException("Usuario y contraseña son obligatorios.");
        }

        var user = await users.FindByUsernameAsync(
            command.Username.Trim(),
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            throw new DomainValidationException(InvalidCredentialsMessage);
        }

        var token = tokenService.CreateToken(user);

        return new LoginResult(
            token.AccessToken,
            token.ExpiresAt,
            user.Username,
            user.Role);
    }
}
