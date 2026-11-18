using InterviewDrill.Core.Vectors;

namespace InterviewDrill.Core.Tests.Math;

public class VectorMathTests
{
    [Fact]
    public void CosineSimilarity_IdenticalVectors_IsOne()
    {
        var left = new float[] { 1, 2, 3 };
        var right = new float[] { 1, 2, 3 };

        Assert.Equal(1.0, VectorMath.CosineSimilarity(left, right), precision: 12);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_IsZero()
    {
        var left = new float[] { 1, 0 };
        var right = new float[] { 0, 1 };

        Assert.Equal(0.0, VectorMath.CosineSimilarity(left, right), precision: 12);
    }

    [Fact]
    public void IsSemanticDuplicate_VeryCloseVectors_ReturnsTrue()
    {
        var left = new float[] { 1, 0 };
        var right = new float[] { 0.99f, 0.01f };

        Assert.True(VectorMath.IsSemanticDuplicate(left, right));
    }

    [Fact]
    public void IsSemanticDuplicate_BelowThreshold_ReturnsFalse()
    {
        var left = new float[] { 1, 0 };
        var right = new float[] { 0.5f, 0.866f };

        Assert.False(VectorMath.IsSemanticDuplicate(left, right));
    }

    [Fact]
    public void IsSemanticDuplicate_MatchesProjectThreshold()
    {
        Assert.Equal(0.92, VectorMath.SemanticDuplicateThreshold);
    }

    [Fact]
    public void CosineSimilarity_MismatchedLengths_Throws()
    {
        Assert.Throws<ArgumentException>(() => VectorMath.CosineSimilarity([1, 2], [1, 2, 3]));
    }

    [Fact]
    public void CosineSimilarity_EmptyVectors_Throws()
    {
        Assert.Throws<ArgumentException>(() => VectorMath.CosineSimilarity([], []));
    }

    [Fact]
    public void CosineSimilarity_ZeroNormVector_ReturnsZero()
    {
        var left = new float[] { 0, 0 };
        var right = new float[] { 1, 1 };

        Assert.Equal(0.0, VectorMath.CosineSimilarity(left, right), precision: 12);
    }

    [Fact]
    public void CosineSimilarity_OppositeVectors_IsNegativeOne()
    {
        var left = new float[] { 1, 0 };
        var right = new float[] { -1, 0 };

        Assert.Equal(-1.0, VectorMath.CosineSimilarity(left, right), precision: 12);
    }
}
