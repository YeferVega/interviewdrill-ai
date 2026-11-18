using System.Security.Cryptography;
using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Infrastructure.Llm;

/// <summary>
/// Deterministic offline embedding generator for development and heuristic fallback.
/// It is not a semantic embedding provider, but it keeps the vector pipeline functional until Azure OpenAI/OpenAI/Bedrock is configured.
/// </summary>
public sealed class HashEmbeddingGenerator : IEmbeddingGenerator
{
    public const int Dimension = 1536;

    public Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var seed = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text));
        var vector = new float[Dimension];
        var rng = new Random(GetInt32(seed, 0));

        for (var i = 0; i < vector.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            vector[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        Normalize(vector);
        return Task.FromResult(vector);
    }

    private static void Normalize(float[] vector)
    {
        var norm = 0.0;
        for (var i = 0; i < vector.Length; i++)
        {
            norm += vector[i] * (double)vector[i];
        }

        if (norm == 0)
        {
            vector[0] = 1;
            return;
        }

        norm = Math.Sqrt(norm);
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= (float)norm;
        }
    }

    private static int GetInt32(byte[] bytes, int offset)
    {
        return BitConverter.ToInt32(bytes.AsSpan(offset, 4));
    }
}
