using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProctorIQ.Application.Results;

namespace ProctorIQ.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
public class ResultsController(IResultService results) : ControllerBase
{
    [HttpGet("{attemptId:guid}")]
    public async Task<IActionResult> Get(Guid attemptId, CancellationToken ct)
    {
        var isAdmin = User.IsInRole("Admin");
        return Ok(await results.GetResultAsync(attemptId, User.UserId(), isAdmin, ct));
    }

    [HttpPost("{examId:guid}/publish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Publish(Guid examId, CancellationToken ct)
    {
        await results.PublishAsync(examId, ct);
        return NoContent();
    }
}
