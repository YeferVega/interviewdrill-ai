using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents.Tests;

public class DifficultyControllerTests
{
    [Theory]
    [InlineData(Seniority.Junior, Difficulty.Easy)]
    [InlineData(Seniority.Mid, Difficulty.Medium)]
    [InlineData(Seniority.Senior, Difficulty.Hard)]
    [InlineData(Seniority.Lead, Difficulty.Expert)]
    [InlineData(Seniority.Unknown, Difficulty.Medium)]
    public void Initial_MatchesSeniority(Seniority seniority, Difficulty expected)
    {
        Assert.Equal(expected, DifficultyController.Initial(seniority));
    }

    [Fact]
    public void Next_Pass_IncreasesDifficulty()
    {
        var next = DifficultyController.Next(Difficulty.Medium, Evaluation(Verdict.Pass));
        Assert.Equal(Difficulty.Hard, next);
    }

    [Fact]
    public void Next_NeedsPractice_MaintainsDifficulty()
    {
        var next = DifficultyController.Next(Difficulty.Hard, Evaluation(Verdict.NeedsPractice));
        Assert.Equal(Difficulty.Hard, next);
    }

    [Fact]
    public void Next_Fail_DecreasesDifficulty()
    {
        var next = DifficultyController.Next(Difficulty.Hard, Evaluation(Verdict.Fail));
        Assert.Equal(Difficulty.Medium, next);
    }

    [Fact]
    public void Next_PassAtExpert_StaysExpert()
    {
        var next = DifficultyController.Next(Difficulty.Expert, Evaluation(Verdict.Pass));
        Assert.Equal(Difficulty.Expert, next);
    }

    [Fact]
    public void Next_FailAtEasy_StaysEasy()
    {
        var next = DifficultyController.Next(Difficulty.Easy, Evaluation(Verdict.Fail));
        Assert.Equal(Difficulty.Easy, next);
    }

    private static EvaluationPayload Evaluation(Verdict verdict) => new()
    {
        OverallScore = verdict == Verdict.Pass ? 90 : verdict == Verdict.NeedsPractice ? 65 : 45,
        TechnicalDepth = 50,
        ProductionReadiness = 50,
        Verdict = verdict,
        CoveredKeywords = [],
        MissedConcepts = [],
        Strengths = [],
        ImprovementAdvice = "test"
    };
}
