using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Core.Scoring;

/// <summary>
/// Determines verdict from overall score.
/// PASS: &gt;= 80 | NEEDS_PRACTICE: 60–79.99 | FAIL: &lt; 60
/// </summary>
public sealed class VerdictDeterminer
{
    private readonly double _passThreshold;
    private readonly double _needsPracticeThreshold;

    public VerdictDeterminer(double passThreshold = 80, double needsPracticeThreshold = 60)
    {
        if (passThreshold <= needsPracticeThreshold)
            throw new ArgumentException(
                $"passThreshold ({passThreshold}) must be greater than needsPracticeThreshold ({needsPracticeThreshold})");

        _passThreshold = passThreshold;
        _needsPracticeThreshold = needsPracticeThreshold;
    }

    public Verdict Determine(double overallScore) => overallScore switch
    {
        _ when overallScore >= _passThreshold => Verdict.Pass,
        _ when overallScore >= _needsPracticeThreshold => Verdict.NeedsPractice,
        _ => Verdict.Fail
    };
}
