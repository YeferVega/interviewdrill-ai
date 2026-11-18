namespace InterviewDrill.Core.Vectors;

/// <summary>
/// Vector math helpers computed in code (Zero-Math principle — never delegated to LLMs).
/// </summary>
public static class VectorMath
{
    public const double SemanticDuplicateThreshold = 0.92;

    public static double CosineSimilarity(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        if (left.Length == 0 || right.Length == 0)
            throw new ArgumentException("Vector must not be empty.", $"{nameof(left)}, {nameof(right)}");

        if (left.Length != right.Length)
            throw new ArgumentException(
                $"Vectors must have the same length; got {left.Length} and {right.Length}.");

        double dot = 0;
        double leftNorm = 0;
        double rightNorm = 0;

        for (int i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftNorm += left[i] * left[i];
            rightNorm += right[i] * right[i];
        }

        if (leftNorm == 0 || rightNorm == 0)
            return 0;

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }

    public static bool IsSemanticDuplicate(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        return CosineSimilarity(left, right) > SemanticDuplicateThreshold;
    }
}
