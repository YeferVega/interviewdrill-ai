namespace InterviewDrill.Core.Llm;

public sealed class LlmUnavailableException : Exception
{
    public LlmUnavailableException()
        : base("The configured LLM provider is unavailable.")
    {
    }

    public LlmUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
