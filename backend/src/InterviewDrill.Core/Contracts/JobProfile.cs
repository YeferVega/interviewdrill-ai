namespace InterviewDrill.Core.Contracts;

/// <summary>
/// Structured profile extracted from a Job Description by the JD Parser Agent.
/// Immutable contract — feeds question generation and study guide prioritization.
/// </summary>
public sealed record JobProfile
{
    public required string Role { get; init; }
    public Seniority Seniority { get; init; } = Seniority.Unknown;
    public required IReadOnlyList<WeightedSkill> RequiredSkills { get; init; }
    public IReadOnlyList<string> NiceToHaveSkills { get; init; } = [];
    public IReadOnlyList<string> Responsibilities { get; init; } = [];

    /// <summary>SHA-256 of normalized JD text. Used for question deduplication per user+JD.</summary>
    public required string RawTextHash { get; init; }

    /// <summary>How the profile was produced ("llm" or "heuristic" fallback).</summary>
    public string ProfileSource { get; init; } = "llm";
}

public sealed record WeightedSkill
{
    public required string Name { get; init; }

    /// <summary>Importance 0.0–1.0 inferred from JD emphasis.</summary>
    public required double Weight { get; init; }
}

public enum Seniority
{
    Unknown,
    Junior,
    Mid,
    Senior,
    Lead
}
