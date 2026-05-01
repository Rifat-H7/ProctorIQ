using ProctorIQ.Application.Auth;

namespace ProctorIQ.Domain.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    TokenPair CreateTokenPair(AppUser user);
}
