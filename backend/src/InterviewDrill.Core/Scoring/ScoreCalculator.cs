namespace InterviewDrill.Core.Scoring;

/// <summary>
/// Computes the weighted overall score in C# (Zero-Math principle — never delegated to LLMs).
/// Formula: overallScore = (TechnicalDepth * 0.6) + (ProductionReadiness * 0.4)
/// </summary>
public sealed class ScoreCalculator
{
    private readonly double _technicalWeight;
    private readonly double _productionWeight;

    public ScoreCalculator(double technicalWeight = 0.6, double productionWeight = 0.4)
    {
        if (Math.Abs(technicalWeight + productionWeight - 1.0) > 1e-9)
            throw new ArgumentException(
                $"Weights must sum to 1.0, got {technicalWeight + productionWeight}");

        _technicalWeight = technicalWeight;
        _productionWeight = productionWeight;
    }

    public double Calculate(double technicalDepth, double productionReadiness)
    {
        ValidateRange(technicalDepth, nameof(technicalDepth));
        ValidateRange(productionReadiness, nameof(productionReadiness));

        var raw = technicalDepth * _technicalWeight + productionReadiness * _productionWeight;
        return Math.Round(Math.Clamp(raw, 0, 100), 2);
    }

    private static void ValidateRange(double score, string paramName)
    {
        if (score < 0 || score > 100)
            throw new ArgumentOutOfRangeException(paramName, score,
                $"{paramName} must be between 0 and 100");
    }
}
