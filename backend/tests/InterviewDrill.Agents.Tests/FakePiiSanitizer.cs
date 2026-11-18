using InterviewDrill.Core.Security;

namespace InterviewDrill.Agents.Tests;

public sealed class FakePiiSanitizer : IPiiSanitizer
{
    public PiiSanitizationResult Sanitize(string? input, bool mask = true)
    {
        return new PiiSanitizationResult(input ?? string.Empty, Array.Empty<PiiFinding>());
    }

    public void AddPattern(PiiPatternDefinition definition)
    {
    }
}
