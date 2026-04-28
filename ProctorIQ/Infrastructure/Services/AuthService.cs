using Microsoft.EntityFrameworkCore;
using ProctorIQ.Application.Auth;
using ProctorIQ.Domain;
using ProctorIQ.Domain.Abstractions;
using ProctorIQ.Infrastructure.Data;

namespace ProctorIQ.Infrastructure.Services;

public class AuthService(AppDbContext db, IPasswordHasher hasher, ITokenService tokens) : IAuthService
{
    public async Task<TokenPair> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var exists = await db.Users.AnyAsync(x => x.Email == request.Email, ct);
        if (exists) throw new InvalidOperationException("Email already exists.");
        var role = Enum.TryParse<UserRole>(request.Role, true, out var r) ? r : UserRole.Candidate;
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = hasher.Hash(request.Password),
            Role = role
        };
        var pair = tokens.CreateTokenPair(user);
        user.RefreshToken = pair.RefreshToken;
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return pair;
    }

    public async Task<TokenPair> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email.ToLower(), ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");
        if (!hasher.Verify(request.Password, user.PasswordHash)) throw new UnauthorizedAccessException("Invalid credentials.");
        var pair = tokens.CreateTokenPair(user);
        user.RefreshToken = pair.RefreshToken;
        await db.SaveChangesAsync(ct);
        return pair;
    }

    public async Task<TokenPair> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email.ToLower(), ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh request.");
        if (user.RefreshToken != request.RefreshToken) throw new UnauthorizedAccessException("Invalid refresh request.");
        var pair = tokens.CreateTokenPair(user);
        user.RefreshToken = pair.RefreshToken;
        await db.SaveChangesAsync(ct);
        return pair;
    }
}
