using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProctorIQ.Infrastructure.Data;
using ProctorIQ.RealTime;

namespace ProctorIQ.Infrastructure.Services;

public class ExamTimerService(IServiceScopeFactory scopeFactory, IHubContext<ExamHub> hub) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            var liveExams = await db.Exams.Where(x => x.StartTimeUtc <= now && x.EndTimeUtc >= now).ToListAsync(stoppingToken);
            foreach (var exam in liveExams)
            {
                var seconds = (int)Math.Max(0, (exam.EndTimeUtc - now).TotalSeconds);
                await hub.Clients.Group($"{exam.Id}:Candidate").SendAsync("TimerTick", exam.Id, seconds, stoppingToken);
            }

            var expired = await db.ExamAttempts
                .Where(x => x.Status == Domain.AttemptStatus.InProgress && db.Exams.Any(e => e.Id == x.ExamId && e.EndTimeUtc < now))
                .ToListAsync(stoppingToken);
            foreach (var attempt in expired)
            {
                await AttemptGradingHelper.GradeAttemptAsync(db, attempt, stoppingToken);
                attempt.Status = Domain.AttemptStatus.Expired;
                attempt.SessionStatus = Domain.AttemptSessionStatus.Expired;
            }
            if (expired.Count > 0) await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
