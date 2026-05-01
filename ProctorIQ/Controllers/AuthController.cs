using Microsoft.AspNetCore.Mvc;
using ProctorIQ.Application.Auth;

namespace ProctorIQ.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService service) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) =>
        Ok(await service.RegisterAsync(request, ct));

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await service.LoginAsync(request, ct));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken ct) =>
        Ok(await service.RefreshAsync(request, ct));
}
