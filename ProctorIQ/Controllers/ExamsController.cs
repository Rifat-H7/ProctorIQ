using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProctorIQ.Application.Exams;

namespace ProctorIQ.Controllers;

[ApiController]
[Route("api/exams")]
[Authorize]
public class ExamsController(IExamService exams) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Proctor")]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await exams.ListExamsAsync(ct));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateExamRequest request, CancellationToken ct) =>
        Ok(new { examId = await exams.CreateExamAsync(request, User.UserId(), ct) });

    [HttpPost("{examId:guid}/questions")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddQuestion(Guid examId, AddQuestionRequest request, CancellationToken ct)
    {
        var model = request with { ExamId = examId };
        return Ok(new { questionId = await exams.AddQuestionAsync(model, ct) });
    }

    [HttpGet("{examId:guid}/questions")]
    [Authorize(Roles = "Candidate")]
    public async Task<IActionResult> CandidateQuestions(Guid examId, CancellationToken ct) =>
        Ok(await exams.GetQuestionsForCandidateAsync(examId, ct));

    [HttpDelete("{examId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid examId, CancellationToken ct)
    {
        await exams.SoftDeleteExamAsync(examId, ct);
        return NoContent();
    }
}
