using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProctorIQ.Application.Results;

namespace ProctorIQ.Controllers;

[ApiController]
[Route("api/certificates")]
[Authorize(Roles = "Candidate,Admin")]
public class CertificatesController(ICertificateService certs) : ControllerBase
{
    [HttpGet("{attemptId:guid}")]
    public async Task<IActionResult> Get(Guid attemptId, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        var result = await certs.GenerateAsync(attemptId, User.UserId(), isAdmin, ct);
        Response.Headers.ContentDisposition = $"attachment; filename={result.FileName}";
        Response.Headers["X-Verification-Code"] = result.VerificationCode;
        return File(result.Pdf, "application/pdf");
    }
}
