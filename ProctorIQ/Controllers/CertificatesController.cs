using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProctorIQ.Infrastructure.Data;

namespace ProctorIQ.Controllers;

[ApiController]
[Route("api/certificates")]
[Authorize(Roles = "Candidate,Admin")]
public class CertificatesController(AppDbContext db) : ControllerBase
{
    [HttpGet("{attemptId:guid}")]
    public async Task<IActionResult> Get(Guid attemptId, CancellationToken ct)
    {
        var data = await db.ExamAttempts.Where(x => x.Id == attemptId)
            .Select(x => new { x.Id, x.Score, x.Percentage, x.IsPassed, x.SubmittedAtUtc })
            .FirstOrDefaultAsync(ct);
        if (data is null) return NotFound();
        if (data.IsPassed != true) return BadRequest("Certificate can be generated only for passed attempts.");
        return Ok(new
        {
            message = "Certificate generation endpoint ready. Plug QuestPDF here.",
            verificationCode = $"PQ-{attemptId.ToString()[..8].ToUpperInvariant()}",
            attempt = data
        });
    }
}
