namespace ProctorIQ.Domain;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
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
