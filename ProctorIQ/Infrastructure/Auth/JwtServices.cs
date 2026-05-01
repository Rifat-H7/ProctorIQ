using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProctorIQ.Application.Auth;
using ProctorIQ.Domain;
using ProctorIQ.Domain.Abstractions;

namespace ProctorIQ.Infrastructure.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "ProctorIQ";
    public string Audience { get; set; } = "ProctorIQ.Client";
    public string SigningKey { get; set; } = "replace-this-with-a-long-strong-key";
    public int AccessTokenMinutes { get; set; } = 60;
}

public class JwtTokenService(IOptions<JwtOptions> jwt) : ITokenService
{
    private readonly JwtOptions _jwt = jwt.Value;

    public TokenPair CreateTokenPair(AppUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expires, signingCredentials: creds);
        var access = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenPair(access, Guid.NewGuid().ToString("N"), expires);
    }
}

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
