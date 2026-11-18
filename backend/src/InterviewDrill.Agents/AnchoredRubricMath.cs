namespace InterviewDrill.Agents;

public static class AnchoredRubricMath
{
    public static double Score1To5(this double value, string name)
    {
        if (value < 1 || value > 5 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be between 1 and 5.");
        }

        return value;
    }

    /// <summary>
    /// Converts 1-5 rubric value to 0-100: 1 => 0, 5 => 100.
    /// </summary>
    public static double Normalize(double rubricScore)
    {
        return ((rubricScore - 1) / 4) * 100;
    }

    public static double WeightedNormalized(
        IReadOnlyDictionary<double, double> weightedScores)
    {
        var totalWeight = weightedScores.Sum(pair => pair.Value);

        if (totalWeight <= 0)
        {
            return 0;
        }

        var score = weightedScores.Sum(pair => pair.Key * pair.Value);
        return Math.Clamp(Math.Round(Normalize(score / totalWeight), 2), 0, 100);
    }

    public static double CombineDeterministicAndJudge(double deterministicScore, double judgeScore, double judgeWeight = 0.45)
    {
        var normalizedWeight = Math.Clamp(judgeWeight, 0, 1);
        var combined = (deterministicScore * (1 - normalizedWeight)) + (judgeScore * normalizedWeight);
        return Math.Clamp(Math.Round(combined, 2), 0, 100);
    }
}
