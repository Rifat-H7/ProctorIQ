using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProctorIQ.Application.Exams;

namespace ProctorIQ.Controllers;

[ApiController]
[Route("api/proctor")]
[Authorize(Roles = "Proctor,Admin")]
public class ProctorController(IAttemptService attempts) : ControllerBase
{
    [HttpGet("exams/active")]
    public async Task<IActionResult> ActiveExams(CancellationToken ct) =>
        Ok(await attempts.ListActiveProctorExamsAsync(ct));

    [HttpGet("exams/{examId:guid}/dashboard")]
    public async Task<IActionResult> Dashboard(Guid examId, CancellationToken ct) =>
        Ok(await attempts.GetExamDashboardAsync(examId, ct));

    [HttpGet("exams/{examId:guid}/evidence.csv")]
    public async Task<IActionResult> ExportEvidence(Guid examId, CancellationToken ct)
    {
        var csv = await attempts.ExportEvidenceCsvAsync(examId, ct);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        var fileName = $"evidence-{examId}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }
}
