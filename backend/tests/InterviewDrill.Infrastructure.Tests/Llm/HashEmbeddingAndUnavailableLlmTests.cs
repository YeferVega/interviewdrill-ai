using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Security;
using InterviewDrill.Core.Vectors;
using InterviewDrill.Infrastructure.Llm;
using InterviewDrill.Infrastructure.Security;
using Xunit;

namespace InterviewDrill.Infrastructure.Tests.Llm;

public sealed class HashEmbeddingGeneratorTests
{
    private readonly HashEmbeddingGenerator _sut = new();

    [Fact]
    public async Task GenerateAsync_ReturnsNormalizedVectorOfFixedDimension()
    {
        var v = await _sut.GenerateAsync("¿Cómo escalar HNSW?");

        Assert.Equal(HashEmbeddingGenerator.Dimension, v.Length);
        var norm = Math.Sqrt(v.Sum(x => (double)x * x));
        Assert.Equal(1.0, norm, 3);
    }

    [Fact]
    public async Task GenerateAsync_IsDeterministic()
    {
        var a = await _sut.GenerateAsync("texto estable");
        var b = await _sut.GenerateAsync("texto estable");

        Assert.Equal(a, b);
    }

    [Fact]
    public async Task GenerateAsync_DifferentTexts_ProduceDifferentVectors()
    {
        var a = await _sut.GenerateAsync("alpha");
        var b = await _sut.GenerateAsync("beta");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public async Task GenerateAsync_SameTextCosineSelfEqualsOne()
    {
        var a = await _sut.GenerateAsync("mismo texto");
        var b = await _sut.GenerateAsync("mismo texto");

        Assert.Equal(1.0, VectorMath.CosineSimilarity(a, b), 6);
    }

    [Fact]
    public async Task GenerateAsync_EmptyString_StillReturnsValidNormalizedVector()
    {
        var v = await _sut.GenerateAsync(string.Empty);

        Assert.Equal(HashEmbeddingGenerator.Dimension, v.Length);
        var norm = Math.Sqrt(v.Sum(x => (double)x * x));
        Assert.Equal(1.0, norm, 3);
    }

    [Fact]
    public async Task GenerateAsync_NullText_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.GenerateAsync(null!));
    }

    [Fact]
    public async Task GenerateAsync_CancellationRequested_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.GenerateAsync("algo largo para que el bucle recorra al menos un paso", cts.Token));
    }
}

public sealed class UnavailableLlmCompletionClientTests
{
    [Fact]
    public async Task CompleteAsync_AlwaysThrowsLlmUnavailable()
    {
        var client = new UnavailableLlmCompletionClient();

        var ex = await Assert.ThrowsAsync<LlmUnavailableException>(
            () => client.CompleteAsync(new LlmCompletionRequest("sys", "user")));

        Assert.NotNull(ex.InnerException);
        Assert.Contains("No LLM provider is configured", ex.Message);
    }
}

public sealed class BedrockLlmClientTests
{
    [Fact]
    public async Task CompleteAsync_WithMissingModelId_ThrowsInvalidOperation()
    {
        var client = new BedrockLlmClient(new HttpClient(), new BedrockOptions { ModelId = string.Empty });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.CompleteAsync(new LlmCompletionRequest("sys", "user")));

        Assert.Contains("LLM:Bedrock:ModelId", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_WithConfiguredModelId_ThrowsNotImplementedForSigV4()
    {
        var client = new BedrockLlmClient(new HttpClient(), new BedrockOptions { ModelId = "anthropic.claude-3-sonnet-20240229-v1:0" });

        var ex = await Assert.ThrowsAsync<NotImplementedException>(
            () => client.CompleteAsync(new LlmCompletionRequest("sys", "user")));

        Assert.Contains("AWS Signature Version 4", ex.Message);
    }
}
