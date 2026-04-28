namespace ProctorIQ.Application.Results;

public interface IResultService
{
    Task<object> GetResultAsync(Guid attemptId, Guid userId, bool isAdmin, CancellationToken ct);
    Task PublishAsync(Guid examId, CancellationToken ct);
}
