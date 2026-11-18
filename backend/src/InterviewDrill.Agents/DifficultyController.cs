using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents;

/// <summary>
/// Pure C# difficulty adaptation. Never delegated to LLMs.
/// PASS increases difficulty, NEEDS_PRACTICE maintains, FAIL decreases.
/// </summary>
public static class DifficultyController
{
    public static Difficulty Initial(Seniority seniority) => seniority switch
    {
        Seniority.Junior => Difficulty.Easy,
        Seniority.Mid => Difficulty.Medium,
        Seniority.Senior => Difficulty.Hard,
        Seniority.Lead => Difficulty.Expert,
        _ => Difficulty.Medium
    };

    public static Difficulty Next(Difficulty current, EvaluationPayload evaluation)
    {
        return evaluation.Verdict switch
        {
            Verdict.Pass => Increase(current),
            Verdict.NeedsPractice => current,
            Verdict.Fail => Decrease(current),
            _ => current
        };
    }

    public static Difficulty Increase(Difficulty current) => current switch
    {
        Difficulty.Easy => Difficulty.Medium,
        Difficulty.Medium => Difficulty.Hard,
        Difficulty.Hard => Difficulty.Expert,
        Difficulty.Expert => Difficulty.Expert,
        _ => Difficulty.Medium
    };

    public static Difficulty Decrease(Difficulty current) => current switch
    {
        Difficulty.Expert => Difficulty.Hard,
        Difficulty.Hard => Difficulty.Medium,
        Difficulty.Medium => Difficulty.Easy,
        Difficulty.Easy => Difficulty.Easy,
        _ => Difficulty.Medium
    };
}
