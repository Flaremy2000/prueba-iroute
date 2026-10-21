using IRoute.Application.Auth;

namespace IRoute.Application.Abstractions;

public interface ITokenService
{
    TokenResult CreateToken(UserAccount user);
}
