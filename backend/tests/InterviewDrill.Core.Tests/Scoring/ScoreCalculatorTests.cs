using InterviewDrill.Core.Scoring;

namespace InterviewDrill.Core.Tests.Scoring;

public class ScoreCalculatorTests
{
    private readonly ScoreCalculator _sut = new();

    [Theory]
    [InlineData(100, 100, 100)]
    [InlineData(0, 0, 0)]
    [InlineData(80, 80, 80)]
    [InlineData(100, 0, 60)]   // 100*0.6 + 0*0.4
    [InlineData(0, 100, 40)]   // 0*0.6 + 100*0.4
    [InlineData(90, 70, 82)]   // 54 + 28
    [InlineData(70, 60, 66)]   // 42 + 24
    [InlineData(50, 50, 50)]
    public void Calculate_WeightedFormula_ReturnsExpected(
        double technical, double production, double expected)
    {
        var result = _sut.Calculate(technical, production);
        Assert.Equal(expected, result, precision: 2);
    }

    [Fact]
    public void Calculate_Boundary60_IsExact()
    {
        // technical=60, production=60 → exactly 60.0 (NEEDS_PRACTICE boundary)
        Assert.Equal(60.0, _sut.Calculate(60, 60), precision: 2);
    }

    [Fact]
    public void Calculate_Boundary79_99_StaysBelowPass()
    {
        // 79.99 must NOT round up to 80
        var result = _sut.Calculate(79.99, 79.99);
        Assert.True(result < 80, $"Expected < 80, got {result}");
    }

    [Theory]
    [InlineData(-10, 50)]
    [InlineData(50, -10)]
    [InlineData(150, 50)]
    [InlineData(50, 150)]
    public void Calculate_OutOfRange_Throws(double technical, double production)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Calculate(technical, production));
    }

    [Fact]
    public void Constructor_WeightsNotSummingToOne_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ScoreCalculator(0.5, 0.3));
    }

    [Fact]
    public void Constructor_CustomWeights_AppliesCorrectly()
    {
        var custom = new ScoreCalculator(technicalWeight: 0.7, productionWeight: 0.3);
        // 80*0.7 + 70*0.3 = 56 + 21 = 77
        Assert.Equal(77.0, custom.Calculate(80, 70), precision: 2);
    }
}
