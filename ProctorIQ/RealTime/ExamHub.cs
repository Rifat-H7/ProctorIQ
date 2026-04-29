using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProctorIQ.Infrastructure.Data;

namespace ProctorIQ.RealTime;

[Authorize]
public class ExamHub(AppDbContext db) : Hub
{
    public Task JoinExam(Guid examId, string role) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"{examId}:{role}");

    public async Task TabSwitchDetected(Guid attemptId, int switchCount)
    {
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId);
        if (attempt is null) return;
        attempt.TabSwitchCount = switchCount;
        db.ProctorLogs.Add(new Domain.ProctorLog
        {
            AttemptId = attempt.Id,
            ProctorId = Guid.Empty,
            EventType = "TabSwitch",
            EventDetail = $"{{\"switchCount\":{switchCount}}}"
        });
        await db.SaveChangesAsync();
        await Clients.Group($"{attempt.ExamId}:Proctor").SendAsync("TabSwitchDetected", attempt.CandidateId, switchCount, DateTime.UtcNow);
    }

    [Authorize(Roles = "Proctor")]
    public async Task ProctorWarning(Guid attemptId, string message)
    {
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId);
        if (attempt is null) return;
        db.ProctorLogs.Add(new Domain.ProctorLog
        {
            AttemptId = attempt.Id,
            ProctorId = Guid.TryParse(Context.UserIdentifier, out var id) ? id : Guid.Empty,
            EventType = "Warning",
            EventDetail = message
        });
        await db.SaveChangesAsync();
        await Clients.User(attempt.CandidateId.ToString()).SendAsync("ProctorWarning", message);
    }

    [Authorize(Roles = "Proctor")]
    public async Task TerminateSession(Guid attemptId, string reason)
    {
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId);
        if (attempt is null) return;
        attempt.Status = Domain.AttemptStatus.Terminated;
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        db.ProctorLogs.Add(new Domain.ProctorLog
        {
            AttemptId = attempt.Id,
            ProctorId = Guid.TryParse(Context.UserIdentifier, out var id) ? id : Guid.Empty,
            EventType = "Terminated",
            EventDetail = reason
        });
        await db.SaveChangesAsync();
        await Clients.User(attempt.CandidateId.ToString()).SendAsync("TerminateSession", reason);
    }
}
