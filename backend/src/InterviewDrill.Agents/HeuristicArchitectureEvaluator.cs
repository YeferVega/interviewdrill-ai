using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Agents;

/// <summary>
/// Deterministic pre-filter evaluator. The full hybrid design still expects an LLM judge for semantic quality.
/// This version is intentionally offline-capable so the interview loop can function without a paid LLM.
/// </summary>
public sealed class HeuristicArchitectureEvaluator(IPiiSanitizer piiSanitizer) : IArchitectureEvaluator
{
    private static readonly (string Name, string[] Terms)[] Concepts =
    [
        ("RAG", ["rag", "retrieval augmented", "retrieval-augmented", "vector store"]),
        ("SemanticKernel", ["semantic kernel", "kernelfunction", "kernel function"]),
        ("HNSW", ["hnsw", "vector search", "vector similarity", "approximate nearest", "embeddings"]),
        ("Kafka", ["kafka", "event streaming", "message queue", "pub/sub"]),
        ("Observability", ["observability", "opentelemetry", "otel", "distributed tracing", "trace", "metrics", "logs"]),
        ("Polly", ["polly", "circuit breaker", "retry", "backoff", "timeout", "resilience"]),
        ("PIISanitization", ["pii", "data masking", "sanitization"]),
        ("RateLimiting", ["429", "rate limit", "throttle"]),
        ("LatencyP95P99", ["p95", "p99", "latency"]),
        ("Guardrails", ["guardrail", "relevance", "human-in-the-loop"])
    ];

    private static readonly (string Name, string Term, int Penalty)[] AntiPatterns =
    [
        ("DelegatingMathToLLM", "use llm for math", 15),
        ("HardcodedSecrets", "hardcode api key", 20),
        ("IgnoringPIIProtection", "ignore pii", 15),
        ("NoRateLimitingHandling", "ignore rate limit", 10),
        ("NoLoggingOrMonitoring", "no monitoring", 10),
        ("StrongProviderCoupling", "only use openai", 5)
    ];

    public Task<ArchitectureEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default)
    {
        var sanitized = piiSanitizer.Sanitize(response.Text);
        var text = sanitized.Text.ToLowerInvariant();

        var matches = Concepts
            .Where(c => c.Terms.Any(term => text.Contains(term, StringComparison.Ordinal)))
            .Select(c => c.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var target = response.SkillTarget;
        var targetCovered = false;

        if (!string.IsNullOrWhiteSpace(target))
        {
            var targetTerms = target.Split(
                [' ', '\t', '\n', '\r', ',', ';', '/'],
                StringSplitOptions.RemoveEmptyEntries)
                .Where(part => part.Length > 2)
                .Select(part => part.ToLowerInvariant())
                .ToList();

            targetCovered = targetTerms.Any(term => text.Contains(term, StringComparison.Ordinal))
                || matches.Contains(target, StringComparer.OrdinalIgnoreCase);

            if (targetCovered && !matches.Contains(target, StringComparer.OrdinalIgnoreCase))
            {
                matches.Add(target!);
            }
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new ArchitectureEvaluation
            {
                TechnicalDepth = 0,
                CoveredKeywords = [],
                MissedConcepts = string.IsNullOrWhiteSpace(target) ? [] : [target],
                Strengths = [],
                DetectedPatterns = [],
                AntiPatterns = []
            });
        }

        var antiPatterns = AntiPatterns
            .Where(a => text.Contains(a.Term, StringComparison.Ordinal))
            .ToList();

        var words = text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
        var sentences = text.Split(['.', '?', '!', '\n'], StringSplitOptions.RemoveEmptyEntries).Count(s => s.Trim().Length > 12);

        var technicalDepth = 40.0;
        technicalDepth += Math.Min(30.0, matches.Count * 6.0);
        if (targetCovered)
        {
            technicalDepth += 20.0;
        }
        if (words > 150)
        {
            technicalDepth += 10.0;
        }
        if (words > 400)
        {
            technicalDepth += 5.0;
        }
        if (sentences >= 3)
        {
            technicalDepth += 5.0;
        }

        technicalDepth -= antiPatterns.Sum(a => a.Penalty);
        technicalDepth = Math.Clamp(Math.Round(technicalDepth, MidpointRounding.AwayFromZero), 0, 100);

        var strengths = new List<string>();
        if (targetCovered)
        {
            strengths.Add($"Addresses the requested skill: {target}.");
        }
        if (matches.Count >= 2)
        {
            strengths.Add("Uses concrete technical concepts instead of vague wording.");
        }
        if (sentences >= 3)
        {
            strengths.Add("Answer is structured and sufficiently detailed.");
        }

        var missed = matches.Count == 0 && !string.IsNullOrWhiteSpace(target)
            ? new List<string> { target! }
            : new List<string>();

        return Task.FromResult(new ArchitectureEvaluation
        {
            TechnicalDepth = technicalDepth,
            CoveredKeywords = matches,
            MissedConcepts = missed,
            Strengths = strengths,
            DetectedPatterns = matches,
            AntiPatterns = antiPatterns.Select(a => a.Name).ToList()
        });
    }
}
