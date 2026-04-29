namespace ProctorIQ.Application.Results;

public interface ICertificateService
{
    Task<(byte[] Pdf, string FileName, string VerificationCode)> GenerateAsync(Guid attemptId, Guid requesterId, bool isAdmin, CancellationToken ct);
}
