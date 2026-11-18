using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InterviewDrill.Infrastructure.Persistence;

public class InterviewDrillDbContext(DbContextOptions<InterviewDrillDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<JobDescriptionEntity> JobDescriptions => Set<JobDescriptionEntity>();
    public DbSet<InterviewSessionEntity> InterviewSessions => Set<InterviewSessionEntity>();
    public DbSet<QuestionEntity> Questions => Set<QuestionEntity>();
    public DbSet<AnswerEntity> Answers => Set<AnswerEntity>();
    public DbSet<StudyGuideEntity> StudyGuides => Set<StudyGuideEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.BrowserToken).IsRequired();
            entity.HasIndex(u => u.BrowserToken).IsUnique();
        });

        modelBuilder.Entity<JobDescriptionEntity>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.Property(j => j.RawTextHash).IsRequired();
            entity.Property(j => j.ParsedProfileJson).IsRequired();
            entity.HasIndex(j => new { j.UserId, j.RawTextHash }).IsUnique();

            entity.HasOne(j => j.User)
                .WithMany(u => u.JobDescriptions)
                .HasForeignKey(j => j.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InterviewSessionEntity>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Status).HasConversion<string>();

            entity.HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.JobDescription)
                .WithMany(j => j.Sessions)
                .HasForeignKey(s => s.JobDescriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuestionEntity>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Text).IsRequired();
            entity.Property(q => q.UserId).IsRequired();
            entity.Property(q => q.JobDescriptionHash).IsRequired();
            entity.Property(q => q.NormalizedQuestionHash).IsRequired();
            entity.Property(q => q.Difficulty).HasConversion<string>();
            entity.Property(q => q.Embedding).HasColumnType("varbinary(max)");

            entity.HasIndex(q => new { q.UserId, q.JobDescriptionHash, q.NormalizedQuestionHash }).IsUnique();
            entity.HasIndex(q => new { q.SessionId, q.AskedAt });

            entity.HasOne(q => q.Session)
                .WithMany(s => s.Questions)
                .HasForeignKey(q => q.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnswerEntity>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Text).IsRequired();
            entity.Property(a => a.EvaluationPayloadJson).IsRequired();

            entity.HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudyGuideEntity>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Markdown).IsRequired();
            entity.HasIndex(g => g.SessionId).IsUnique();

            entity.HasOne(g => g.Session)
                .WithOne(s => s.StudyGuide)
                .HasForeignKey<StudyGuideEntity>(g => g.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
