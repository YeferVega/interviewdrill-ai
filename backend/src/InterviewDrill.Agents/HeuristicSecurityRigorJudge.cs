using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Agents;

/// <summary>
/// Deterministic production-readiness judge. It detects explicit guardrails, privacy and resilience signals.
/// Full LLM semantic judging remains pending for Phase 3 hardening.
/// </summary>
public sealed class HeuristicSecurityRigorJudge(IPiiSanitizer piiSanitizer) : ISecurityRigorJudge
{
    private static readonly (string Name, string[] Terms)[] Concepts =
    [
        ("PIISanitization", ["pii", "data masking", "sanitize", "anonymiz"]),
        ("RelevanceGuardrails", ["guardrail", "relevance", "threshold", "0.78"]),
        ("HumanInTheLoop", ["human-in-the-loop", "human review", "manual review"]),
        ("RateLimiting", ["429", "rate limit", "throttle", "backpressure"]),
        ("LatencyP95P99", ["p95", "p99", "latency budget"]),
        ("SemanticCache", ["semantic cache", "llm cache", "response cache"]),
        ("Observability", ["observability", "monitoring", "opentelemetry", "otel", "logs", "metrics", "trace"]),
        ("DataProtection", ["encryption", "least privilege", "secret", "authentication", "authorization"]),
        ("DeploymentSafety", ["rollback", "feature flag", "canary", "blue-green", "dr"])
    ];

    private static readonly string[] ResilienceTerms =
    [
        "retry", "backoff", "circuit breaker", "polly", "timeout", "fallback", "dead letter", "idempot", "queue"
    ];

    public Task<SecurityEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default)
    {
        var sanitized = piiSanitizer.Sanitize(response.Text);
        var text = sanitized.Text.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new SecurityEvaluation
            {
                ProductionReadiness = 0,
                ResilienceScore = 0,
                CoveredKeywords = [],
                MissedConcepts = Concepts.Select(c => c.Name).ToList(),
                Strengths = [],
                SecurityConcerns = ["Answer is empty."]
            });
        }

        var covered = Concepts
            .Where(c => c.Terms.Any(term => text.Contains(term, StringComparison.Ordinal)))
            .Select(c => c.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resilienceCount = ResilienceTerms.Count(term => text.Contains(term, StringComparison.Ordinal));

        var penalty = 0;
        if (text.Contains("no sanitization", StringComparison.Ordinal) ||
            text.Contains("ignore pii", StringComparison.Ordinal))
        {
            penalty += 18;
        }

        if (text.Contains("no monitoring", StringComparison.Ordinal))
        {
            penalty += 10;
        }

        if (text.Contains("hardcode api key", StringComparison.Ordinal))
        {
            penalty += 20;
        }

        var productionReadiness = Math.Clamp(35 + (covered.Count * 9) + (resilienceCount * 6) - penalty, 0, 100);
        var resilienceScore = Math.Clamp(30 + (resilienceCount * 12) - penalty, 0, 100);

        var criticalTerms = new[]
        {
            "PIISanitization",
            "Guardrails",
            "RateLimiting",
            "LatencyP95P99",
            "Observability",
            "DataProtection",
            "DeploymentSafety"
        };
        var missed = covered.Count == 0
            ? criticalTerms.ToList()
            : Concepts
                .Select(c => c.Name)
                .Where(name => !covered.Contains(name, StringComparer.OrdinalIgnoreCase))
                .Where(criticalTerms.Contains)
                .ToList();

        var strengths = new List<string>();
        if (covered.Contains("PIISanitization", StringComparer.OrdinalIgnoreCase))
        {
            strengths.Add("Considers PII protection before inference or persistence.");
        }
        if (resilienceCount >= 2)
        {
            strengths.Add("Uses concrete resilience mechanisms such as retry, circuit breaker, timeout or fallback.");
        }
        if (covered.Count >= 3)
        {
            strengths.Add("Demonstrates broad production readiness.");
        }

        var concerns = missed.Count > 0
            ? new List<string> { $"Missing production controls: {string.Join(", ", missed)}." }
            : [];

        if (penalty > 0)
        {
            concerns.Add("Answer contains statements that weaken production safety.");
        }

        return Task.FromResult(new SecurityEvaluation
        {
            ProductionReadiness = productionReadiness,
            ResilienceScore = resilienceScore,
            CoveredKeywords = covered,
            MissedConcepts = missed,
            Strengths = strengths,
            SecurityConcerns = concerns
        });
    }
}
