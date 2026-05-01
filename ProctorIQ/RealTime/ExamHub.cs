using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ProctorIQ.Infrastructure.Data;

namespace ProctorIQ.RealTime;

[Authorize]
public class ExamHub(AppDbContext db) : Hub
{
    private const int SuspiciousTabSwitchThreshold = 3;
    private static readonly HashSet<string> SuspiciousLockdownSignals = new(StringComparer.OrdinalIgnoreCase)
    {
        "ForbiddenShortcut",
        "FullscreenExit",
        "DevToolsAttempt"
    };

    public Task JoinExam(Guid examId, string role) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"{examId}:{role}");

    [Authorize(Roles = "Candidate")]
    public async Task JoinAttempt(Guid attemptId)
    {
        var userId = GetUserId();
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId && x.CandidateId == userId);
        if (attempt is null) return;
        attempt.LastSeenAtUtc = DateTime.UtcNow;
        attempt.CurrentConnectionId = Context.ConnectionId;
        attempt.SessionStatus = Domain.AttemptSessionStatus.Connected;
        await db.SaveChangesAsync();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"{attempt.ExamId}:Candidate");
        await Clients.Group($"{attempt.ExamId}:Proctor").SendAsync("CandidateStatusUpdated", attempt.CandidateId, attempt.SessionStatus.ToString(), attempt.LastSeenAtUtc);
    }

    [Authorize(Roles = "Candidate")]
    public async Task CandidateHeartbeat(Guid attemptId)
    {
        var userId = GetUserId();
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId && x.CandidateId == userId);
        if (attempt is null) return;
        attempt.LastSeenAtUtc = DateTime.UtcNow;
        if (attempt.Status == Domain.AttemptStatus.InProgress && attempt.SessionStatus != Domain.AttemptSessionStatus.Suspicious)
            attempt.SessionStatus = Domain.AttemptSessionStatus.Connected;
        await db.SaveChangesAsync();
    }

    public async Task TabSwitchDetected(Guid attemptId, int switchCount)
    {
        var userId = GetUserId();
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId && x.CandidateId == userId);
        if (attempt is null) return;
        attempt.TabSwitchCount = switchCount;
        attempt.LastSeenAtUtc = DateTime.UtcNow;
        if (switchCount >= SuspiciousTabSwitchThreshold)
        {
            attempt.SessionStatus = Domain.AttemptSessionStatus.Suspicious;
        }
        db.ProctorLogs.Add(new Domain.ProctorLog
        {
            AttemptId = attempt.Id,
            ProctorId = Guid.Empty,
            EventType = "TabSwitch",
            EventDetail = $"{{\"switchCount\":{switchCount}}}"
        });
        await db.SaveChangesAsync();
        await Clients.Group($"{attempt.ExamId}:Proctor").SendAsync("TabSwitchDetected", attempt.CandidateId, switchCount, DateTime.UtcNow);
        if (attempt.SessionStatus == Domain.AttemptSessionStatus.Suspicious)
        {
            await Clients.Group($"{attempt.ExamId}:Proctor").SendAsync("CandidateStatusUpdated", attempt.CandidateId, attempt.SessionStatus.ToString(), attempt.LastSeenAtUtc);
        }
    }

    [Authorize(Roles = "Candidate")]
    public async Task LockdownSignal(Guid attemptId, string signalType, string? detail)
    {
        var userId = GetUserId();
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId && x.CandidateId == userId);
        if (attempt is null || attempt.Status != Domain.AttemptStatus.InProgress) return;

        attempt.LastSeenAtUtc = DateTime.UtcNow;
        var atUtc = attempt.LastSeenAtUtc.Value;
        var normalizedSignalType = string.IsNullOrWhiteSpace(signalType) ? "Unknown" : signalType.Trim();
        var safeDetail = string.IsNullOrWhiteSpace(detail) ? string.Empty : detail.Trim();
        var isSuspicious = SuspiciousLockdownSignals.Contains(normalizedSignalType);
        if (isSuspicious)
        {
            attempt.SessionStatus = Domain.AttemptSessionStatus.Suspicious;
        }

        db.ProctorLogs.Add(new Domain.ProctorLog
        {
            AttemptId = attempt.Id,
            ProctorId = Guid.Empty,
            EventType = "LockdownSignal",
            EventDetail = $"{{\"signalType\":\"{normalizedSignalType}\",\"detail\":\"{safeDetail.Replace("\"", "'")}\"}}"
        });
        await db.SaveChangesAsync();

        await Clients.Group($"{attempt.ExamId}:Proctor")
            .SendAsync("LockdownSignalDetected", attempt.CandidateId, attempt.Id, normalizedSignalType, safeDetail, atUtc, attempt.SessionStatus.ToString());

        if (isSuspicious)
        {
            await Clients.Group($"{attempt.ExamId}:Proctor")
                .SendAsync("CandidateStatusUpdated", attempt.CandidateId, attempt.SessionStatus.ToString(), atUtc);
        }
    }

    [Authorize(Roles = "Proctor")]
    public async Task ProctorWarning(Guid attemptId, string message)
    {
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId);
        if (attempt is null) return;
        attempt.WarningCount += 1;
        attempt.SessionStatus = Domain.AttemptSessionStatus.Suspicious;
        db.ProctorLogs.Add(new Domain.ProctorLog
        {
            AttemptId = attempt.Id,
            ProctorId = Guid.TryParse(Context.UserIdentifier, out var id) ? id : Guid.Empty,
            EventType = "Warning",
            EventDetail = message
        });
        await db.SaveChangesAsync();
        await Clients.User(attempt.CandidateId.ToString()).SendAsync("ProctorWarning", message);
        await Clients.Group($"{attempt.ExamId}:Proctor").SendAsync("CandidateStatusUpdated", attempt.CandidateId, attempt.SessionStatus.ToString(), attempt.LastSeenAtUtc);
    }

    [Authorize(Roles = "Proctor")]
    public async Task TerminateSession(Guid attemptId, string reason)
    {
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.Id == attemptId);
        if (attempt is null) return;
        attempt.Status = Domain.AttemptStatus.Terminated;
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        attempt.SessionStatus = Domain.AttemptSessionStatus.Terminated;
        db.ProctorLogs.Add(new Domain.ProctorLog
        {
            AttemptId = attempt.Id,
            ProctorId = Guid.TryParse(Context.UserIdentifier, out var id) ? id : Guid.Empty,
            EventType = "Terminated",
            EventDetail = reason
        });
        await db.SaveChangesAsync();
        await Clients.User(attempt.CandidateId.ToString()).SendAsync("TerminateSession", reason);
        await Clients.Group($"{attempt.ExamId}:Proctor").SendAsync("CandidateStatusUpdated", attempt.CandidateId, attempt.SessionStatus.ToString(), attempt.SubmittedAtUtc);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var attempt = await db.ExamAttempts.FirstOrDefaultAsync(x => x.CurrentConnectionId == Context.ConnectionId);
        if (attempt is not null && attempt.Status == Domain.AttemptStatus.InProgress)
        {
            attempt.SessionStatus = Domain.AttemptSessionStatus.Disconnected;
            attempt.LastSeenAtUtc = DateTime.UtcNow;
            attempt.CurrentConnectionId = null;
            db.ProctorLogs.Add(new Domain.ProctorLog
            {
                AttemptId = attempt.Id,
                ProctorId = Guid.Empty,
                EventType = "Disconnected",
                EventDetail = "Candidate connection dropped"
            });
            await db.SaveChangesAsync();
            await Clients.Group($"{attempt.ExamId}:Proctor")
                .SendAsync("CandidateStatusUpdated", attempt.CandidateId, attempt.SessionStatus.ToString(), attempt.LastSeenAtUtc);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Guid GetUserId()
    {
        var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
