using System.Text.Json.Serialization;

namespace InterviewDrill.Infrastructure.Persistence.Entities;

public sealed class JobDescriptionEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string UserId { get; set; } = string.Empty;
    public UserEntity User { get; set; } = default!;

    public string RawTextHash { get; set; } = string.Empty;
    public string ParsedProfileJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<InterviewSessionEntity> Sessions { get; set; } = [];
}
