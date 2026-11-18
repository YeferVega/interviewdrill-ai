using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Infrastructure.Persistence.Entities;

public sealed class UserEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BrowserToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<JobDescriptionEntity> JobDescriptions { get; set; } = [];
    public ICollection<InterviewSessionEntity> Sessions { get; set; } = [];
}
