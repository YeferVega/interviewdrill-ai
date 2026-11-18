using System.Text.Json.Serialization;

namespace InterviewDrill.Infrastructure.Persistence.Entities;

public enum InterviewSessionStatus
{
    InProgress,
    Completed,
    Aborted
}

public sealed class InterviewSessionEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = string.Empty;
    public UserEntity User { get; set; } = default!;

    public string JobDescriptionId { get; set; } = string.Empty;
    public JobDescriptionEntity JobDescription { get; set; } = default!;

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public InterviewSessionStatus Status { get; set; } = InterviewSessionStatus.InProgress;
    public string? InterviewerStateJson { get; set; }

    public ICollection<QuestionEntity> Questions { get; set; } = [];
    public StudyGuideEntity? StudyGuide { get; set; }
}
