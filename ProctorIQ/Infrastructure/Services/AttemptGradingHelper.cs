using Microsoft.EntityFrameworkCore;
using ProctorIQ.Domain;
using ProctorIQ.Infrastructure.Data;

namespace ProctorIQ.Infrastructure.Services;

internal static class AttemptGradingHelper
{
    public static async Task GradeAttemptAsync(AppDbContext db, ExamAttempt attempt, CancellationToken ct)
    {
        var exam = await db.Exams.FirstAsync(x => x.Id == attempt.ExamId, ct);
        var answers = await db.CandidateAnswers.Where(x => x.AttemptId == attempt.Id).ToListAsync(ct);
        decimal total = 0;
        foreach (var ans in answers)
        {
            var correct = await db.QuestionOptions.AnyAsync(
                o => o.QuestionId == ans.QuestionId && o.Id == ans.SelectedOptionId && o.IsCorrect, ct);
            var marks = await db.Questions.Where(q => q.Id == ans.QuestionId).Select(q => q.Marks).FirstAsync(ct);
            ans.IsCorrect = correct;
            ans.MarksAwarded = correct ? marks : 0;
            total += ans.MarksAwarded;
        }

        attempt.Score = total;
        attempt.Percentage = exam.TotalMarks == 0 ? 0 : Math.Round((total / exam.TotalMarks) * 100, 2);
        attempt.IsPassed = total >= exam.PassMarks;
        attempt.SubmittedAtUtc = DateTime.UtcNow;
    }
}
