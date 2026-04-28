using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProctorIQ.Application.Exams;
using ProctorIQ.Application.Results;
using ProctorIQ.Domain;
using ProctorIQ.Infrastructure.Data;
using ProctorIQ.RealTime;

namespace ProctorIQ.Infrastructure.Services;

public class ExamService(AppDbContext db) : IExamService
{
    public async Task<Guid> CreateExamAsync(CreateExamRequest request, Guid adminId, CancellationToken ct)
    {
        var exam = new Exam
        {
            Title = request.Title,
            CreatedByUserId = adminId,
            DurationMinutes = request.DurationMinutes,
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            TotalMarks = request.TotalMarks,
            PassMarks = request.PassMarks,
            IsRandomised = request.IsRandomised,
            Status = ExamStatus.Scheduled
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(ct);
        return exam.Id;
    }

    public async Task<List<object>> ListExamsAsync(CancellationToken ct) =>
        await db.Exams.Select(x => (object)new { x.Id, x.Title, x.Status, x.StartTimeUtc, x.EndTimeUtc }).ToListAsync(ct);

    public async Task<Guid> AddQuestionAsync(AddQuestionRequest request, CancellationToken ct)
    {
        var qType = request.Type.Equals("TrueFalse", StringComparison.OrdinalIgnoreCase) ? QuestionType.TrueFalse : QuestionType.Mcq;
        var q = new Question
        {
            ExamId = request.ExamId,
            QuestionText = request.QuestionText,
            Type = qType,
            Marks = request.Marks,
            Difficulty = request.Difficulty,
            Topic = request.Topic,
            Options = request.Options.Select(o => new QuestionOption { OptionText = o.OptionText, IsCorrect = o.IsCorrect }).ToList()
        };
        db.Questions.Add(q);
        await db.SaveChangesAsync(ct);
        return q.Id;
    }

    public async Task<List<object>> GetQuestionsForCandidateAsync(Guid examId, CancellationToken ct)
    {
        var exam = await db.Exams.Include(x => x.Questions).ThenInclude(x => x.Options).FirstAsync(x => x.Id == examId, ct);
        var ordered = exam.IsRandomised ? exam.Questions.OrderBy(_ => Guid.NewGuid()).ToList() : exam.Questions;
        return ordered.Select(q => (object)new
        {
            q.Id,
            q.QuestionText,
            q.Type,
            q.Marks,
            Options = q.Options.Select(o => new { o.Id, o.OptionText })
        }).ToList();
    }
}

public class AttemptService(AppDbContext db) : IAttemptService
{
    public async Task<Guid> StartAttemptAsync(StartAttemptRequest request, Guid candidateId, CancellationToken ct)
    {
        var exists = await db.ExamAttempts.AnyAsync(x => x.ExamId == request.ExamId && x.CandidateId == candidateId, ct);
        if (exists) throw new InvalidOperationException("Attempt already exists.");
        var attempt = new ExamAttempt { ExamId = request.ExamId, CandidateId = candidateId };
        db.ExamAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);
        return attempt.Id;
    }

    public async Task SaveAnswerAsync(Guid attemptId, SaveAnswerRequest request, Guid candidateId, CancellationToken ct)
    {
        var attempt = await db.ExamAttempts.FirstAsync(x => x.Id == attemptId && x.CandidateId == candidateId, ct);
        if (attempt.Status != AttemptStatus.InProgress) throw new InvalidOperationException("Attempt is closed.");
        var answer = await db.CandidateAnswers.FirstOrDefaultAsync(x => x.AttemptId == attemptId && x.QuestionId == request.QuestionId, ct);
        if (answer is null)
        {
            answer = new CandidateAnswer { AttemptId = attemptId, QuestionId = request.QuestionId };
            db.CandidateAnswers.Add(answer);
        }
        answer.SelectedOptionId = request.SelectedOptionId;
        await db.SaveChangesAsync(ct);
    }

    public async Task SubmitAttemptAsync(Guid attemptId, Guid candidateId, CancellationToken ct)
    {
        var attempt = await db.ExamAttempts.FirstAsync(x => x.Id == attemptId && x.CandidateId == candidateId, ct);
        var exam = await db.Exams.FirstAsync(x => x.Id == attempt.ExamId, ct);
        var answers = await db.CandidateAnswers.Where(x => x.AttemptId == attemptId).ToListAsync(ct);
        decimal total = 0;
        foreach (var ans in answers)
        {
            var correct = await db.QuestionOptions.AnyAsync(o => o.QuestionId == ans.QuestionId && o.Id == ans.SelectedOptionId && o.IsCorrect, ct);
            var marks = await db.Questions.Where(q => q.Id == ans.QuestionId).Select(q => q.Marks).FirstAsync(ct);
            ans.IsCorrect = correct;
            ans.MarksAwarded = correct ? marks : 0;
            total += ans.MarksAwarded;
        }
        attempt.Score = total;
        attempt.Percentage = exam.TotalMarks == 0 ? 0 : Math.Round((total / exam.TotalMarks) * 100, 2);
        attempt.IsPassed = total >= exam.PassMarks;
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        attempt.Status = AttemptStatus.Submitted;
        await db.SaveChangesAsync(ct);
    }
}

public class ResultService(AppDbContext db, IHubContext<ExamHub> hub) : IResultService
{
    public async Task<object> GetResultAsync(Guid attemptId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var q = db.ExamAttempts.Where(x => x.Id == attemptId);
        if (!isAdmin) q = q.Where(x => x.CandidateId == userId);
        var attempt = await q.Select(x => new { x.Id, x.ExamId, x.Score, x.Percentage, x.IsPassed, x.Status }).FirstAsync(ct);
        return attempt;
    }

    public async Task PublishAsync(Guid examId, CancellationToken ct)
    {
        var submitted = await db.ExamAttempts.Where(x => x.ExamId == examId && x.Status == AttemptStatus.Submitted).ToListAsync(ct);
        foreach (var x in submitted)
        {
            await hub.Clients.User(x.CandidateId.ToString())
                .SendAsync("ResultPublished", examId, x.CandidateId, x.Score, x.IsPassed, ct);
        }
    }
}
