namespace InterviewDrill.Core.Contracts;

public sealed record ArchitectureJudgeResult(
    string Reasoning,
    double Clarity,
    double TechnicalAccuracy,
    double ArchitectureDepth,
    double Communication,
    IReadOnlyList<string> CoveredKeywords,
    IReadOnlyList<string> MissedConcepts,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> DetectedAntiPatterns);

public sealed record SecurityJudgeResult(
    string Reasoning,
    double PiiProtection,
    double RelevanceGuardrails,
    double Observability,
    double Resilience,
    double DeploymentSafety,
    IReadOnlyList<string> CoveredKeywords,
    IReadOnlyList<string> MissedConcepts,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> SecurityConcerns);
