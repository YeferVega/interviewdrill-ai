namespace InterviewDrill.Core.Llm;

public sealed record LlmCompletionRequest(
    string SystemPrompt,
    string UserPrompt,
    string? ResponseJsonSchema = null,
    double Temperature = 0.0,
    string? CorrelationId = null);

public interface ILlmCompletionClient
{
    Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default);
}
