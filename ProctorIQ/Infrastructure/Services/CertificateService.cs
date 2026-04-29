using Microsoft.EntityFrameworkCore;
using ProctorIQ.Application.Results;
using ProctorIQ.Domain;
using ProctorIQ.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProctorIQ.Infrastructure.Services;

public class CertificateService(AppDbContext db) : ICertificateService
{
    public async Task<(byte[] Pdf, string FileName, string VerificationCode)> GenerateAsync(Guid attemptId, Guid requesterId, bool isAdmin, CancellationToken ct)
    {
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId, ct)
            ?? throw new InvalidOperationException("Attempt not found.");
        if (!isAdmin && attempt.CandidateId != requesterId) throw new UnauthorizedAccessException("Not allowed.");
        if (attempt.IsPassed != true) throw new InvalidOperationException("Only passed attempts can receive certificate.");

        var exam = await db.Exams.FirstAsync(x => x.Id == attempt.ExamId, ct);
        var user = await db.Users.FirstAsync(x => x.Id == attempt.CandidateId, ct);
        var cert = await db.Certificates.FirstOrDefaultAsync(x => x.AttemptId == attemptId, ct);
        if (cert is null)
        {
            cert = new Certificate
            {
                AttemptId = attempt.Id,
                CandidateId = attempt.CandidateId,
                VerificationCode = $"PQ-{attempt.Id.ToString("N")[..10].ToUpperInvariant()}"
            };
            db.Certificates.Add(cert);
            await db.SaveChangesAsync(ct);
        }

        QuestPDF.Settings.License = LicenseType.Community;
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(14));
                page.Content().Column(col =>
                {
                    col.Item().Text("ProctorIQ Certificate").FontSize(28).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().PaddingTop(10).Text($"This certifies that {user.FullName}");
                    col.Item().Text($"successfully passed \"{exam.Title}\"");
                    col.Item().Text($"Score: {attempt.Score} ({attempt.Percentage}%)");
                    col.Item().Text($"Issued: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
                    col.Item().Text($"Verification Code: {cert.VerificationCode}").Bold();
                });
            });
        }).GeneratePdf();

        return (bytes, $"certificate-{attempt.Id:N}.pdf", cert.VerificationCode);
    }
}
