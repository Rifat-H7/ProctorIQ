using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProctorIQ.Application.Exams;

namespace ProctorIQ.Controllers;

[ApiController]
[Route("api/attempts")]
[Authorize(Roles = "Candidate")]
public class AttemptsController(IAttemptService attempts) : ControllerBase
{
    [HttpPost("start")]
    public async Task<IActionResult> Start(StartAttemptRequest request, CancellationToken ct) =>
        Ok(new { attemptId = await attempts.StartAttemptAsync(request, User.UserId(), ct) });

    [HttpPut("{attemptId:guid}/answer")]
    public async Task<IActionResult> Save(Guid attemptId, SaveAnswerRequest request, CancellationToken ct)
    {
        await attempts.SaveAnswerAsync(attemptId, request, User.UserId(), ct);
        return NoContent();
    }

    [HttpPost("{attemptId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid attemptId, CancellationToken ct)
    {
        await attempts.SubmitAttemptAsync(attemptId, User.UserId(), ct);
        return NoContent();
    }
}
