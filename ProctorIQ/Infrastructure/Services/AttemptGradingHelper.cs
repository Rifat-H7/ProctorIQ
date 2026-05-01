using Microsoft.EntityFrameworkCore;
using ProctorIQ.Domain;
using ProctorIQ.Infrastructure.Data;

namespace ProctorIQ.Infrastructure.Services;

internal static class AttemptGradingHelper
{
    public static async Task GradeAttemptAsync(AppDbContext db, ExamAttempt attempt, CancellationToken ct)
    {
        var exam = await db.Exams.FirstAsync(x => x.Id == attempt.ExamId, ct);
        var questionMeta = await db.Questions
            .Where(x => x.ExamId == attempt.ExamId)
            .Select(x => new { x.Id, x.Type, x.Marks })
            .ToDictionaryAsync(x => x.Id, ct);
        var answers = await db.CandidateAnswers.Where(x => x.AttemptId == attempt.Id).ToListAsync(ct);
        decimal total = 0;
        var hasWritten = questionMeta.Values.Any(q => q.Type == QuestionType.Written);
        foreach (var ans in answers)
        {
            if (!questionMeta.TryGetValue(ans.QuestionId, out var meta)) continue;
            if (meta.Type == QuestionType.Written)
            {
                ans.IsCorrect = false;
                ans.MarksAwarded = 0;
                continue;
            }

            var correct = await db.QuestionOptions.AnyAsync(
                o => o.QuestionId == ans.QuestionId && o.Id == ans.SelectedOptionId && o.IsCorrect, ct);
            ans.IsCorrect = correct;
            ans.MarksAwarded = correct ? meta.Marks : 0;
            total += ans.MarksAwarded;
        }

        attempt.Score = total;
        if (hasWritten)
        {
            attempt.Percentage = null;
            attempt.IsPassed = null;
        }
        else
        {
            attempt.Percentage = exam.TotalMarks == 0 ? 0 : Math.Round((total / exam.TotalMarks) * 100, 2);
            attempt.IsPassed = total >= exam.PassMarks;
        }
        attempt.SubmittedAtUtc = DateTime.UtcNow;
    }
}
