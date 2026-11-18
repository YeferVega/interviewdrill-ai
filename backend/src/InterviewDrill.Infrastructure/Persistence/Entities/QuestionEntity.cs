using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Infrastructure.Persistence.Entities;

public sealed class QuestionEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string SessionId { get; set; } = string.Empty;
    public InterviewSessionEntity Session { get; set; } = default!;

    public string UserId { get; set; } = string.Empty;
    public string JobDescriptionHash { get; set; } = string.Empty;
    public string NormalizedQuestionHash { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public string SkillTarget { get; set; } = string.Empty;

    public byte[]? Embedding { get; set; }

    public DateTimeOffset AskedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<AnswerEntity> Answers { get; set; } = [];
}
