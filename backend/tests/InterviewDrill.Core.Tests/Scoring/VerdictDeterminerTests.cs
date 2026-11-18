using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Scoring;

namespace InterviewDrill.Core.Tests.Scoring;

public class VerdictDeterminerTests
{
    private readonly VerdictDeterminer _sut = new();

    [Theory]
    [InlineData(100, Verdict.Pass)]
    [InlineData(80, Verdict.Pass)]        // exact boundary
    [InlineData(95.5, Verdict.Pass)]
    [InlineData(79.99, Verdict.NeedsPractice)]  // just below PASS
    [InlineData(60, Verdict.NeedsPractice)]     // exact boundary
    [InlineData(70, Verdict.NeedsPractice)]
    [InlineData(59.99, Verdict.Fail)]     // just below NEEDS_PRACTICE
    [InlineData(0, Verdict.Fail)]
    [InlineData(30, Verdict.Fail)]
    public void Determine_AllBoundaries_ReturnsExpectedVerdict(double score, Verdict expected)
    {
        Assert.Equal(expected, _sut.Determine(score));
    }

    [Fact]
    public void Constructor_InvalidThresholds_Throws()
    {
        Assert.Throws<ArgumentException>(() => new VerdictDeterminer(60, 80));
    }
}
