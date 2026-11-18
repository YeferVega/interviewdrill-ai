namespace InterviewDrill.Core.Contracts;

/// <summary>
/// Immutable output contract of the evaluation pipeline (mirrors EvaluationPayload JSON Schema in AGENTS.md).
/// </summary>
public sealed record EvaluationPayload
{
    public required double OverallScore { get; init; }
    public required double TechnicalDepth { get; init; }
    public required double ProductionReadiness { get; init; }
    public required Verdict Verdict { get; init; }
    public required IReadOnlyList<string> CoveredKeywords { get; init; }
    public required IReadOnlyList<string> MissedConcepts { get; init; }
    public required IReadOnlyList<string> Strengths { get; init; }
    public required string ImprovementAdvice { get; init; }
}

public enum Verdict
{
    Fail,
    NeedsPractice,
    Pass
}

/// <summary>Evaluation produced by the Architecture Evaluator agent.</summary>
public sealed record ArchitectureEvaluation
{
    public required double TechnicalDepth { get; init; }
    public required IReadOnlyList<string> CoveredKeywords { get; init; }
    public required IReadOnlyList<string> MissedConcepts { get; init; }
    public required IReadOnlyList<string> Strengths { get; init; }
    public IReadOnlyList<string> DetectedPatterns { get; init; } = [];
    public IReadOnlyList<string> AntiPatterns { get; init; } = [];
}

/// <summary>Evaluation produced by the Security &amp; Rigor Judge agent.</summary>
public sealed record SecurityEvaluation
{
    public required double ProductionReadiness { get; init; }
    public required IReadOnlyList<string> CoveredKeywords { get; init; }
    public required IReadOnlyList<string> MissedConcepts { get; init; }
    public required IReadOnlyList<string> Strengths { get; init; }
    public IReadOnlyList<string> SecurityConcerns { get; init; } = [];
    public required double ResilienceScore { get; init; }
}

/// <summary>Candidate's answer to an interview question.</summary>
public sealed record CandidateResponse
{
    public required string SessionId { get; init; }
    public required string QuestionId { get; init; }
    public required string Text { get; init; }

    /// <summary>Skill being evaluated by the current question. Used by heuristic/semantic judges.</summary>
    public string? SkillTarget { get; init; }

    /// <summary>Free-text output language for judge prose ("es" | "en"). Null keeps current behavior.</summary>
    public string? Language { get; init; }
}

/// <summary>A generated interview question targeting a skill from the JobProfile.</summary>
public sealed record InterviewQuestion
{
    public required string Id { get; init; }
    public required string Text { get; init; }
    public required string SkillTarget { get; init; }
    public required Difficulty Difficulty { get; init; }
    public QuestionType Type { get; init; } = QuestionType.Situational;
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}

public enum QuestionType
{
    Conceptual,
    Situational,
    FailureScenario,
    FollowUp
}
