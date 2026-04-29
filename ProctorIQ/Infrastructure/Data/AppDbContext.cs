using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProctorIQ.Domain;

namespace ProctorIQ.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<CandidateAnswer> CandidateAnswers => Set<CandidateAnswer>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ProctorLog> ProctorLogs => Set<ProctorLog>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>().ToTable("Users");
        b.Entity<IdentityRole<Guid>>().ToTable("Roles");
        b.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        b.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        b.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        b.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        b.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<AppUser>().Property(x => x.FullName).HasMaxLength(100);

        b.Entity<Exam>().HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.ExamId);
        b.Entity<Question>().HasMany(x => x.Options).WithOne().HasForeignKey(x => x.QuestionId);
        b.Entity<ExamAttempt>().HasMany(x => x.Answers).WithOne().HasForeignKey(x => x.AttemptId);
        b.Entity<QuestionOption>().HasIndex(x => new { x.QuestionId, x.IsCorrect });
        b.Entity<ExamAttempt>().HasIndex(x => new { x.ExamId, x.CandidateId }).IsUnique();

        b.Entity<RefreshToken>().HasIndex(x => x.Token).IsUnique();
        b.Entity<RefreshToken>().HasOne<AppUser>().WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
        b.Entity<ProctorLog>().HasIndex(x => x.AttemptId);
        b.Entity<Certificate>().HasIndex(x => x.AttemptId).IsUnique();
        b.Entity<Certificate>().HasIndex(x => x.VerificationCode).IsUnique();
    }
}
