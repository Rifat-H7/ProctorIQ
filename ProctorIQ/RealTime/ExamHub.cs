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
        await db.SaveChangesAsync();
        await Clients.Group($"{attempt.ExamId}:Proctor").SendAsync("TabSwitchDetected", attempt.CandidateId, switchCount, DateTime.UtcNow);
    }
}
