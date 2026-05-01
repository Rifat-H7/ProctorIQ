using System.Security.Claims;

namespace ProctorIQ.Controllers;

public static class ControllerAuthExtensions
{
    public static Guid UserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
