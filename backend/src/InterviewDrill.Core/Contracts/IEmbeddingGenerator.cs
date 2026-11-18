namespace InterviewDrill.Core.Contracts;

/// <summary>
/// Provider-agnostic embedding generator.
/// Implementations may target Azure OpenAI, OpenAI, Bedrock, Ollama, or a deterministic fake generator.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>Generates a fixed-size float embedding for similarity search.</summary>
    Task<float[]> GenerateAsync(string text, CancellationToken ct = default);
}
