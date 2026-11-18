using System.Text.RegularExpressions;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Infrastructure.Security;

public sealed class RegexPiiSanitizer : IPiiSanitizer
{
    private readonly List<CompiledPattern> _patterns;

    public RegexPiiSanitizer()
    {
        _patterns =
        [
            new CompiledPattern("CreditCard", "[CREDIT_CARD]", new Regex(@"\b(?:\d{4}[ -]?){3,4}\b", RegexOptions.Compiled), 100),
            new CompiledPattern("SSN", "[SSN]", new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled), 100),
            new CompiledPattern("Email", "[EMAIL]", new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled), 90),
            new CompiledPattern("IPAddress", "[IP]", new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled), 80),
            new CompiledPattern("IPv6Address", "[IP]", new Regex(@"\b(?:[0-9a-fA-F]{1,4}:){2,7}[0-9a-fA-F]{1,4}\b", RegexOptions.Compiled), 80),
            new CompiledPattern("Phone", "[PHONE]", new Regex(@"\+?\d{1,3}[ .-]?\(?\d{3}\)?[ .-]?\d{3}[ .-]?\d{4}", RegexOptions.Compiled), 70),
            new CompiledPattern("Name", "[NAME]", new Regex(@"(?<=\b(?:name|nombre|contact|contacto|candidato|candidate|aplicante|applicant)\s*:\s*)[\p{Lu}][\p{L}-]+(?:\s+[\p{Lu}][\p{L}-]+)+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), 60),
            new CompiledPattern("Address", "[ADDRESS]", new Regex(@"\b\d{1,6}\s+(?:[A-ZÁÉÍÓÚ][\p{L}-]+\s+){1,4}(?:Street|St\.?|Avenue|Ave\.?|Road|Rd\.?|Boulevard|Blvd\.?|Drive|Dr\.?|Lane|Ln\.?|Calle|Avenida|Carretera|Camino)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase), 60)
        ];
    }

    public PiiSanitizationResult Sanitize(string? input, bool mask = true)
    {
        if (string.IsNullOrEmpty(input))
        {
            return new PiiSanitizationResult(input ?? string.Empty, Array.Empty<PiiFinding>());
        }

        var matches = new List<PiiFinding>();

        foreach (var pattern in _patterns)
        {
            foreach (Match match in pattern.Regex.Matches(input))
            {
                if (match.Index < 0 || match.Length == 0)
                {
                    continue;
                }

                matches.Add(new PiiFinding(
                    pattern.Type,
                    pattern.Placeholder,
                    match.Index,
                    match.Length,
                    pattern.Priority));
            }
        }

        var ordered = matches
            .OrderBy(m => m.Index)
            .ThenByDescending(m => m.Priority)
            .ThenByDescending(m => m.Length);

        var selected = new List<PiiFinding>();

        foreach (var match in ordered)
        {
            var overlaps = selected.Any(existing =>
                match.Index < existing.Index + existing.Length &&
                existing.Index < match.Index + match.Length);

            if (!overlaps)
            {
                selected.Add(match);
            }
        }

        var final = selected
            .OrderBy(m => m.Index)
            .ToList();

        if (!mask)
        {
            return new PiiSanitizationResult(input, final);
        }

        var builder = new System.Text.StringBuilder(input.Length);
        var cursor = 0;

        foreach (var finding in final)
        {
            builder.Append(input, cursor, finding.Index - cursor);
            builder.Append(finding.Placeholder);
            cursor = finding.Index + finding.Length;
        }

        builder.Append(input, cursor, input.Length - cursor);

        return new PiiSanitizationResult(builder.ToString(), final);
    }

    public void AddPattern(PiiPatternDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        _patterns.Add(new CompiledPattern(
            definition.Name,
            definition.Placeholder,
            new Regex(definition.Pattern, definition.Options),
            definition.Priority));
    }

    private sealed record CompiledPattern(string Type, string Placeholder, Regex Regex, int Priority);
}
