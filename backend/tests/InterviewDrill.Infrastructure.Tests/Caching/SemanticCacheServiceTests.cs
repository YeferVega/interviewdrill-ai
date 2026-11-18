using InterviewDrill.Core.Caching;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Security;
using InterviewDrill.Infrastructure.Caching;
using InterviewDrill.Infrastructure.Security;
using Xunit;

namespace InterviewDrill.Infrastructure.Tests.Caching;

/// <summary>
/// Fake <see cref="IEmbeddingGenerator"/> que devuelve vectores determinísticos y controlables
/// para probar la lógica de similitud coseno del SemanticCacheService sin depender de un proveedor real.
/// </summary>
public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly Dictionary<string, float[]> _vectors = new(StringComparer.OrdinalIgnoreCase);
    private int _counter;

    public float[] this[string text]
    {
        get => _vectors.TryGetValue(text, out var v) ? v : GenerateVector(text);
        set => _vectors[text] = value;
    }

    public Task<float[]> GenerateAsync(string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_vectors.TryGetValue(text, out var v) ? v : GenerateVector(text));
    }

    private float[] GenerateVector(string text)
    {
        // Vector determinístico basado en el hash del texto, normalizado.
        var hash = 0;
        foreach (var c in text)
        {
            hash = (hash * 31 + c) & 0x7fffffff;
        }

        var rng = new Random(hash);
        var v = new float[8];
        for (var i = 0; i < v.Length; i++)
        {
            v[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        }

        // Normalizar para que similitud coseno de un vector consigo mismo = 1.0
        var norm = 0.0;
        foreach (var x in v)
        {
            norm += x * x;
        }
        norm = Math.Sqrt(norm);
        if (norm == 0) v[0] = 1;
        else for (var i = 0; i < v.Length; i++) v[i] /= (float)norm;

        _vectors[text] = v;
        return v;
    }
}

/// <summary>
/// Fake <see cref="IPiiSanitizer"/> que no modifica el texto (modo passthrough)
/// para aislar la lógica de caché en los tests.
/// </summary>
public sealed class PassthroughPiiSanitizer : IPiiSanitizer
{
    public PiiSanitizationResult Sanitize(string? input, bool mask = true) =>
        new(input ?? string.Empty, []);

    public void AddPattern(PiiPatternDefinition definition) { }
}

public class SemanticCacheServiceTests
{
    private readonly FakeEmbeddingGenerator _embeddingGenerator = new();
    private readonly IPiiSanitizer _piiSanitizer = new RegexPiiSanitizer();

    private SemanticCacheService CreateService() =>
        new(_embeddingGenerator, _piiSanitizer);

    [Fact]
    public async Task GetAsync_OnEmptyCache_ReturnsMiss()
    {
        var service = CreateService();

        var result = await service.GetAsync("¿Cómo manejar la latencia en microservicios?");

        Assert.False(result.Hit);
        Assert.Null(result.Response);
        Assert.Null(result.Similarity);
    }

    [Fact]
    public async Task GetAsync_IdenticalQuery_ReturnsHitWithHighSimilarity()
    {
        var service = CreateService();
        const string query = "¿Cómo implementar rate limiting con Polly?";
        const string response = "Usa Polly con WaitAndRetryAsync.";

        await service.SetAsync(query, response);
        var result = await service.GetAsync(query);

        Assert.True(result.Hit);
        Assert.Equal(response, result.Response);
        // La similitud coseno de un vector consigo mismo es 1.0 (puede ser 1.0000000000000002 por punto flotante)
        Assert.InRange(result.Similarity ?? 0, 0.92, 1.0001);
    }

    [Fact]
    public async Task GetAsync_DifferentQuery_ReturnsMissBelowThreshold()
    {
        var service = CreateService();

        await service.SetAsync("¿Cómo implementar rate limiting con Polly?", "Usa Polly.");
        var result = await service.GetAsync("¿Cuál es la capital de Francia?");

        Assert.False(result.Hit);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task GetAsync_CustomThreshold_BelowThresholdReturnsMiss()
    {
        var service = CreateService();
        const string query = "¿Cómo manejar la latencia?";
        const string response = "Usa caché y colas.";

        await service.SetAsync(query, response);
        // Threshold por encima de 1.0 es imposible de alcanzar para cualquier vector real
        var result = await service.GetAsync(query, similarityThreshold: 1.0001);

        Assert.False(result.Hit);
    }

    [Fact]
    public async Task GetAsync_CustomThreshold_AboveThresholdReturnsHit()
    {
        var service = CreateService();
        const string query = "¿Cómo implementar circuit breaker?";
        const string response = "Usa Polly CircuitBreakerAsync.";

        await service.SetAsync(query, response);
        var result = await service.GetAsync(query, similarityThreshold: 0.5);

        Assert.True(result.Hit);
        Assert.Equal(response, result.Response);
    }

    [Fact]
    public async Task SetAsync_WithTTL_ExpiresAfterConfiguredSeconds()
    {
        var service = CreateService();
        const string query = "¿Qué es HNSW?";
        const string response = "Índice vectorial para búsqueda aproximada.";

        await service.SetAsync(query, response, ttlSeconds: 0);
        var result = await service.GetAsync(query);

        Assert.False(result.Hit);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesEntry()
    {
        var service = CreateService();
        const string query = "¿Cómo usar Kafka?";
        const string response = "Productor y consumidor.";

        await service.SetAsync(query, response);
        var hitBefore = await service.GetAsync(query);
        Assert.True(hitBefore.Hit);

        await service.InvalidateAsync(query);
        var hitAfter = await service.GetAsync(query);
        Assert.False(hitAfter.Hit);
    }

    [Fact]
    public async Task Namespaces_AreIsolated()
    {
        var service = CreateService();
        const string query = "Misma consulta en dos namespaces";

        await service.SetAsync(query, "respuesta A", namespaceName: "ns1");
        await service.SetAsync(query, "respuesta B", namespaceName: "ns2");

        var hit1 = await service.GetAsync(query, namespaceName: "ns1");
        var hit2 = await service.GetAsync(query, namespaceName: "ns2");

        Assert.True(hit1.Hit);
        Assert.Equal("respuesta A", hit1.Response);
        Assert.True(hit2.Hit);
        Assert.Equal("respuesta B", hit2.Response);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsHitAndMissCounts()
    {
        var service = CreateService();

        await service.SetAsync("q1", "r1");
        await service.GetAsync("q1"); // hit
        await service.GetAsync("q2"); // miss
        await service.GetAsync("q1"); // hit

        var stats = await service.GetStatisticsAsync();

        Assert.Equal(3, stats.TotalQueries);
        Assert.Equal(2, stats.Hits);
        Assert.Equal(1, stats.Misses);
        Assert.Equal(2.0 / 3.0, stats.HitRate, 3);
        Assert.Equal(1, stats.TotalEntries);
    }

    [Fact]
    public async Task GetAsync_SanitizesPiiBeforeLookup()
    {
        var service = CreateService();
        const string queryWithPii = "Contacta a alice@example.com sobre latencia";
        const string querySanitized = "Contacta a [EMAIL] sobre latencia";
        const string response = "Usa caché distribuido.";

        // Guardar con el query que contiene PII (debe sanitizarse internamente)
        await service.SetAsync(queryWithPii, response);

        // Buscar con el query ya sanitizado (debe encontrar)
        var result = await service.GetAsync(querySanitized);

        Assert.True(result.Hit);
        Assert.Equal(response, result.Response);
    }
}
