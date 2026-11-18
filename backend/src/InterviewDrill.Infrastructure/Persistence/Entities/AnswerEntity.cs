namespace InterviewDrill.Infrastructure.Persistence.Entities;

public sealed class AnswerEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string QuestionId { get; set; } = string.Empty;
    public QuestionEntity Question { get; set; } = default!;

    public string Text { get; set; } = string.Empty;
    public string EvaluationPayloadJson { get; set; } = "{}";
    public DateTimeOffset AnsweredAt { get; set; } = DateTimeOffset.UtcNow;
}
