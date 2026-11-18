using System.Text.RegularExpressions;

namespace InterviewDrill.Core.Security;

public sealed record PiiPatternDefinition(
    string Name,
    string Placeholder,
    string Pattern,
    RegexOptions Options = RegexOptions.Compiled,
    int Priority = 10);

public sealed record PiiFinding(
    string Type,
    string Placeholder,
    int Index,
    int Length,
    int Priority);

public sealed record PiiSanitizationResult(
    string Text,
    IReadOnlyList<PiiFinding> Findings);

public interface IPiiSanitizer
{
    PiiSanitizationResult Sanitize(string? input, bool mask = true);

    void AddPattern(PiiPatternDefinition definition);
}
