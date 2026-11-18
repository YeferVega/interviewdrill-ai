namespace InterviewDrill.Infrastructure.Persistence.Entities;

public sealed class StudyGuideEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string SessionId { get; set; } = string.Empty;
    public InterviewSessionEntity Session { get; set; } = default!;

    public string Markdown { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
