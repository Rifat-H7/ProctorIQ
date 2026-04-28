using Microsoft.EntityFrameworkCore;
using ProctorIQ.Domain;

namespace ProctorIQ.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<CandidateAnswer> CandidateAnswers => Set<CandidateAnswer>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Exam>().HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.ExamId);
        b.Entity<Question>().HasMany(x => x.Options).WithOne().HasForeignKey(x => x.QuestionId);
        b.Entity<ExamAttempt>().HasMany(x => x.Answers).WithOne().HasForeignKey(x => x.AttemptId);
    }
}
