namespace ProctorIQ.Application.Auth;

public record RegisterRequest(string FullName, string Email, string Password, string Role = "Candidate");
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string Email, string RefreshToken);
public record TokenPair(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);

public interface IAuthService
{
    Task<TokenPair> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<TokenPair> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<TokenPair> RefreshAsync(RefreshRequest request, CancellationToken ct);
}
