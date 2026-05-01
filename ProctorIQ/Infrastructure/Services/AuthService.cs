using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProctorIQ.Application.Auth;
using ProctorIQ.Domain;
using ProctorIQ.Domain.Abstractions;
using ProctorIQ.Infrastructure.Data;

namespace ProctorIQ.Infrastructure.Services;

public class AuthService(
    AppDbContext db,
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ITokenService tokens) : IAuthService
{
    public async Task<TokenPair> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var exists = await userManager.Users.AnyAsync(x => x.Email == normalized, ct);
        if (exists) throw new InvalidOperationException("Email already exists.");
        var role = Enum.TryParse<UserRole>(request.Role, true, out var r) ? r : UserRole.Candidate;
        var user = new AppUser
        {
            FullName = request.FullName,
            UserName = normalized,
            Email = normalized,
            Role = role,
            IsActive = true
        };
        var create = await userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded) throw new InvalidOperationException(string.Join("; ", create.Errors.Select(x => x.Description)));
        var roleName = role.ToString();
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
        await userManager.AddToRoleAsync(user, roleName);
        var pair = await IssueAndPersistRefreshAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return pair;
    }

    public async Task<TokenPair> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Email == normalized, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");
        if (!user.IsActive) throw new UnauthorizedAccessException("Account is deactivated.");
        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid) throw new UnauthorizedAccessException("Invalid credentials.");
        var pair = await IssueAndPersistRefreshAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return pair;
    }

    public async Task<TokenPair> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Email == normalized, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh request.");
        if (!user.IsActive) throw new UnauthorizedAccessException("Account is deactivated.");
        var active = await db.RefreshTokens
            .Where(x => x.UserId == user.Id && x.Token == request.RefreshToken && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (active is null) throw new UnauthorizedAccessException("Invalid refresh request.");
        active.RevokedAtUtc = DateTime.UtcNow;
        var pair = await IssueAndPersistRefreshAsync(user, ct);
        await db.SaveChangesAsync(ct);
        return pair;
    }

    private async Task<TokenPair> IssueAndPersistRefreshAsync(AppUser user, CancellationToken ct)
    {
        var pair = tokens.CreateTokenPair(user);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = pair.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        });
        await db.SaveChangesAsync(ct);
        return pair;
    }
}
