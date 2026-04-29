using Microsoft.AspNetCore.Identity;

namespace ProctorIQ.Domain;

public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}

public class Exam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int TotalMarks { get; set; }
    public int PassMarks { get; set; }
    public bool IsRandomised { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public List<Question> Questions { get; set; } = [];
}

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.Mcq;
    public int Marks { get; set; } = 1;
    public string Difficulty { get; set; } = "Medium";
    public string Topic { get; set; } = string.Empty;
    public List<QuestionOption> Options { get; set; } = [];
}

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class ExamAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }
    public Guid CandidateId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public decimal? Score { get; set; }
    public decimal? Percentage { get; set; }
    public bool? IsPassed { get; set; }
    public int TabSwitchCount { get; set; }
    public int WarningCount { get; set; }
    public AttemptSessionStatus SessionStatus { get; set; } = AttemptSessionStatus.Disconnected;
    public DateTime? LastSeenAtUtc { get; set; }
    public string? CurrentConnectionId { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public List<CandidateAnswer> Answers { get; set; } = [];
}

public class CandidateAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public decimal MarksAwarded { get; set; }
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
}

public class ProctorLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public Guid ProctorId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventDetail { get; set; } = string.Empty;
    public DateTime LoggedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Certificate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public Guid CandidateId { get; set; }
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public string VerificationCode { get; set; } = string.Empty;
    public string? PdfPath { get; set; }
}
