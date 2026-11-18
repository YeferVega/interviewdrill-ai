using InterviewDrill.Agents;

namespace InterviewDrill.Agents.Tests;

public sealed class AnchoredRubricMathTests
{
    [Fact]
    public void Normalize_1_ReturnsZero()
    {
        Assert.Equal(0.0, AnchoredRubricMath.Normalize(1), 6);
    }

    [Fact]
    public void Normalize_5_ReturnsHundred()
    {
        Assert.Equal(100.0, AnchoredRubricMath.Normalize(5), 6);
    }

    [Theory]
    [InlineData(2, 25)]
    [InlineData(3, 50)]
    [InlineData(4, 75)]
    [InlineData(3.5, 62.5)]
    public void Normalize_LinearMapping(double rubric, double expected)
    {
        Assert.Equal(expected, AnchoredRubricMath.Normalize(rubric), 6);
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(5.01)]
    [InlineData(-1)]
    public void Score1To5_OutOfRange_Throws(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Score1To5("score"));
    }

    [Fact]
    public void Score1To5_NanAndInfinity_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => double.NaN.Score1To5("score"));
        Assert.Throws<ArgumentOutOfRangeException>(() => double.PositiveInfinity.Score1To5("score"));
        Assert.Throws<ArgumentOutOfRangeException>(() => double.NegativeInfinity.Score1To5("score"));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(3.0)]
    [InlineData(5.0)]
    public void Score1To5_ValidRange_ReturnsSameValue(double value)
    {
        Assert.Equal(value, value.Score1To5("score"));
    }

    [Fact]
    public void WeightedNormalized_EmptyDictionary_ReturnsZero()
    {
        var result = AnchoredRubricMath.WeightedNormalized(new Dictionary<double, double>());

        Assert.Equal(0, result);
    }

    [Fact]
    public void WeightedNormalized_SingleScore_MapsLinearly()
    {
        // 4 in 1-5 => 75 normalized
        var result = AnchoredRubricMath.WeightedNormalized(new Dictionary<double, double> { [4] = 1 });

        Assert.Equal(75, result, 2);
    }

    [Fact]
    public void WeightedNormalized_WeightedAverageOfRubrics()
    {
        // (3*0.6 + 5*0.4) = 3.8 => (3.8-1)/4*100 = 70
        var result = AnchoredRubricMath.WeightedNormalized(new Dictionary<double, double>
        {
            [3] = 0.6,
            [5] = 0.4
        });

        Assert.Equal(70, result, 2);
    }

    [Fact]
    public void WeightedNormalized_ClampsTo100WhenWeightsInvalid()
    {
        // totalWeight <= 0 => 0
        var result = AnchoredRubricMath.WeightedNormalized(new Dictionary<double, double> { [5] = 0 });

        Assert.Equal(0, result);
    }

    [Fact]
    public void CombineDeterministicAndJudge_WithDefaultWeight()
    {
        // 60*(1-0.45) + 90*0.45 = 33 + 40.5 = 73.5
        var result = AnchoredRubricMath.CombineDeterministicAndJudge(60, 90);

        Assert.Equal(73.5, result, 2);
    }

    [Fact]
    public void CombineDeterministicAndJudge_ZeroWeight_KeepsDeterministic()
    {
        var result = AnchoredRubricMath.CombineDeterministicAndJudge(70, 90, judgeWeight: 0);

        Assert.Equal(70, result, 2);
    }

    [Fact]
    public void CombineDeterministicAndJudge_FullWeight_KeepsJudge()
    {
        var result = AnchoredRubricMath.CombineDeterministicAndJudge(70, 90, judgeWeight: 1);

        Assert.Equal(90, result, 2);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(1.5)]
    public void CombineDeterministicAndJudge_WeightClampedTo01(double weight)
    {
        // Negative or >1 should be clamped; combined stays within 0..100
        var result = AnchoredRubricMath.CombineDeterministicAndJudge(50, 50, judgeWeight: weight);

        Assert.InRange(result, 0, 100);
    }
}
