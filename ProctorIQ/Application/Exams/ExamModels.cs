namespace ProctorIQ.Application.Exams;

public record CreateExamRequest(
    string Title,
    int DurationMinutes,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    int TotalMarks,
    int PassMarks,
    bool IsRandomised);

public record AddQuestionRequest(
    Guid ExamId,
    string QuestionText,
    string Type,
    int Marks,
    string Difficulty,
    string Topic,
    List<OptionRequest> Options);
public record UpdateQuestionRequest(
    string QuestionText,
    string Type,
    int Marks,
    string Difficulty,
    string Topic,
    List<OptionRequest> Options);

public record OptionRequest(string OptionText, bool IsCorrect);
public record StartAttemptRequest(Guid ExamId);
public record SaveAnswerRequest(Guid QuestionId, Guid? SelectedOptionId);
public record AttemptResumeResponse(Guid AttemptId, string Status, Dictionary<Guid, Guid?> Answers);

public interface IExamService
{
    Task<Guid> CreateExamAsync(CreateExamRequest request, Guid adminId, CancellationToken ct);
    Task<List<object>> ListExamsAsync(CancellationToken ct);
    Task<List<object>> ListAvailableExamsAsync(CancellationToken ct);
    Task<Guid> AddQuestionAsync(AddQuestionRequest request, CancellationToken ct);
    Task<List<object>> GetQuestionsForAdminAsync(Guid examId, CancellationToken ct);
    Task UpdateQuestionAsync(Guid examId, Guid questionId, UpdateQuestionRequest request, CancellationToken ct);
    Task<List<object>> GetQuestionsForCandidateAsync(Guid examId, CancellationToken ct);
    Task SoftDeleteExamAsync(Guid examId, CancellationToken ct);
}

public interface IAttemptService
{
    Task<Guid> StartAttemptAsync(StartAttemptRequest request, Guid candidateId, CancellationToken ct);
    Task SaveAnswerAsync(Guid attemptId, SaveAnswerRequest request, Guid candidateId, CancellationToken ct);
    Task SubmitAttemptAsync(Guid attemptId, Guid candidateId, CancellationToken ct);
    Task<AttemptResumeResponse?> GetAttemptForExamAsync(Guid examId, Guid candidateId, CancellationToken ct);
    Task<List<object>> ListActiveProctorExamsAsync(CancellationToken ct);
    Task<object> GetExamDashboardAsync(Guid examId, CancellationToken ct);
    Task<string> ExportEvidenceCsvAsync(Guid examId, CancellationToken ct);
}
