using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Scoring;

namespace InterviewDrill.Agents;

public sealed class ScoringSynthesizer(
    ScoreCalculator? scoreCalculator = null,
    VerdictDeterminer? verdictDeterminer = null) : IScoringSynthesizer
{
    private readonly ScoreCalculator _scoreCalculator = scoreCalculator ?? new ScoreCalculator();
    private readonly VerdictDeterminer _verdictDeterminer = verdictDeterminer ?? new VerdictDeterminer();

    public Task<EvaluationPayload> SynthesizeAsync(
        ArchitectureEvaluation architectureEval,
        SecurityEvaluation securityEval,
        CancellationToken ct = default,
        string? language = null)
    {
        ValidateScore(architectureEval.TechnicalDepth, nameof(architectureEval.TechnicalDepth));
        ValidateScore(securityEval.ProductionReadiness, nameof(securityEval.ProductionReadiness));

        var overallScore = _scoreCalculator.Calculate(
            architectureEval.TechnicalDepth,
            securityEval.ProductionReadiness);

        var verdict = _verdictDeterminer.Determine(overallScore);

        var covered = Distinct(architectureEval.CoveredKeywords, securityEval.CoveredKeywords);
        var missed = Distinct(architectureEval.MissedConcepts, securityEval.MissedConcepts);
        var strengths = Distinct(architectureEval.Strengths, securityEval.Strengths);

        var advice = BuildAdvice(
            verdict,
            missed,
            architectureEval.AntiPatterns,
            securityEval.SecurityConcerns,
            language);

        return Task.FromResult(new EvaluationPayload
        {
            OverallScore = overallScore,
            TechnicalDepth = architectureEval.TechnicalDepth,
            ProductionReadiness = securityEval.ProductionReadiness,
            Verdict = verdict,
            CoveredKeywords = covered,
            MissedConcepts = missed,
            Strengths = strengths,
            ImprovementAdvice = advice
        });
    }

    private static IReadOnlyList<string> Distinct(params IEnumerable<string>[] sources)
    {
        return sources
            .SelectMany(x => x)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildAdvice(
        Verdict verdict,
        IReadOnlyList<string> missed,
        IReadOnlyList<string> antiPatterns,
        IReadOnlyList<string> concerns,
        string? language)
    {
        // "es" selects the Spanish phrase set. Null/other keeps the historical English output.
        var spanish = InterviewLanguages.IsSpanish(language);
        var parts = new List<string>();

        if (verdict == Verdict.Pass)
        {
            parts.Add(spanish
                ? "Respuesta sólida. Mantén la estructura y agrega evidencia medible de producción."
                : "Strong answer. Keep the structure and add measurable production evidence next time.");
        }
        else if (verdict == Verdict.NeedsPractice)
        {
            parts.Add(spanish
                ? "Buena base, pero la respuesta necesita controles de producción más concretos."
                : "Good foundation, but the answer needs more concrete production controls.");
        }
        else
        {
            parts.Add(spanish
                ? "La respuesta necesita más profundidad técnica y seguridad en producción."
                : "The answer needs more technical depth and production safety.");
        }

        if (missed.Count > 0)
        {
            parts.Add(spanish
                ? $"Estudia y menciona: {string.Join(", ", missed)}."
                : $"Study and mention: {string.Join(", ", missed)}.");
        }

        if (antiPatterns.Count > 0)
        {
            parts.Add(spanish
                ? $"Evita estos antipatrones: {string.Join(", ", antiPatterns)}."
                : $"Avoid these anti-patterns: {string.Join(", ", antiPatterns)}.");
        }

        if (concerns.Count > 0)
        {
            parts.Add(spanish
                ? $"Riesgos de producción: {string.Join(", ", concerns)}."
                : $"Production concerns: {string.Join(", ", concerns)}.");
        }

        return string.Join(' ', parts);
    }

    private static void ValidateScore(double value, string name)
    {
        if (value < 0 || value > 100)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be between 0 and 100.");
        }
    }
}
