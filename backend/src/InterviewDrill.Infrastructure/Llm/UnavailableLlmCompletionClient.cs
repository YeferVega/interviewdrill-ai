using InterviewDrill.Core.Llm;

namespace InterviewDrill.Infrastructure.Llm;

public sealed class UnavailableLlmCompletionClient : ILlmCompletionClient
{
    public Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
    {
        return Task.FromException<string>(
            new LlmUnavailableException(
                "No LLM provider is configured yet. Use Azure OpenAI, OpenAI, Bedrock or Ollama adapter.",
                new InvalidOperationException("No LLM adapter registered.")));
    }
}
