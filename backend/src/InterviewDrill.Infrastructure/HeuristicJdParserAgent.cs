using System.Text.RegularExpressions;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Security;

namespace InterviewDrill.Infrastructure;

public sealed class HeuristicJdParserAgent(IPiiSanitizer piiSanitizer) : IJdParserAgent
{
    private static readonly Dictionary<string, string[]> SkillPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RAG"] = ["rag", "retrieval augmented", "retrieval-augmented"],
        ["SemanticKernel"] = ["semantic kernel", "kernelfunction", "kernel function"],
        ["HNSW"] = ["hnsw", "vector search", "vector similarity", "vector database", "embedding"],
        ["Kafka"] = ["kafka", "event streaming"],
        ["Observability"] = ["observability", "opentelemetry", "otel", "distributed tracing", "activitysource", "activity source"],
        ["Polly"] = ["polly", "circuit breaker", "retry policy", "resilience"],
        ["React"] = ["react"],
        ["TypeScript"] = ["typescript"],
        ["EF Core"] = ["ef core", "entity framework core", "entity framework"],
        ["PostgreSQL"] = ["postgres", "postgresql", "pgvector"],
        ["SQL Server"] = ["sql server", "azure sql"],
        ["Azure"] = ["azure"],
        ["AWS"] = ["aws", "amazon web services"],
        ["CI/CD"] = ["ci/cd", "cicd", "continuous integration", "continuous delivery", "continuous deployment"],
        ["CloudFoundry"] = ["cloudfoundry", "cloud foundry", "cf"],
        ["Kubernetes"] = ["kubernetes", "k8s"],
        ["Docker"] = ["docker", "container"],
        ["gRPC"] = ["grpc"]
    };

    public Task<JobProfile> ParseAsync(string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new JdParsingException("Job Description text is empty.");
        }

        var sanitized = piiSanitizer.Sanitize(content).Text;
        var lines = sanitized
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0)
            .ToList();

        var requiredSkills = ExtractSkills(lines, isNiceToHave: false);
        var niceToHaveSkills = ExtractSkills(lines, isNiceToHave: true);

        return Task.FromResult(new JobProfile
        {
            Role = ExtractRole(lines, sanitized),
            Seniority = ExtractSeniority(sanitized),
            RequiredSkills = requiredSkills,
            NiceToHaveSkills = niceToHaveSkills.Select(s => s.Name).ToList(),
            Responsibilities = ExtractResponsibilities(lines),
            RawTextHash = JdTextHashing.ComputeRawTextHash(content),
            ProfileSource = "heuristic"
        });
    }

    private static string ExtractRole(IEnumerable<string> lines, string fullText)
    {
        foreach (var line in lines)
        {
            var match = Regex.Match(
                line,
                @"^(?:role|job title|position|title|puesto)\s*[:#-]\s*(.+)$",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        var firstLine = lines.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstLine) && firstLine.Length <= 120)
        {
            return firstLine;
        }

        var heading = Regex.Match(fullText, @"^(.+?)(?:\n|$)", RegexOptions.Singleline);
        return heading.Success && heading.Groups[1].Value.Length <= 120
            ? heading.Groups[1].Value.Trim()
            : "Software Engineer";
    }

    private static Seniority ExtractSeniority(string text)
    {
        var lower = text.ToLowerInvariant();

        if (Regex.IsMatch(lower, @"\b(lead|principal|staff|architect|director|head)\b"))
        {
            return Seniority.Lead;
        }

        if (Regex.IsMatch(lower, @"\b(senior|sr\.?|high[- ]level)\b"))
        {
            return Seniority.Senior;
        }

        if (Regex.IsMatch(lower, @"\b(mid|intermediate|semi[- ]senior|semisenior)\b"))
        {
            return Seniority.Mid;
        }

        if (Regex.IsMatch(lower, @"\b(junior|entry[- ]level|trainee)\b"))
        {
            return Seniority.Junior;
        }

        return Seniority.Unknown;
    }

    private static IReadOnlyList<WeightedSkill> ExtractSkills(IEnumerable<string> lines, bool isNiceToHave)
    {
        var matches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var lineIsNice = LooksLikeNiceToHave(line);
            if (lineIsNice != isNiceToHave)
            {
                continue;
            }

            var lower = line.ToLowerInvariant();

            foreach (var (skill, patterns) in SkillPatterns)
            {
                if (patterns.Any(pattern => lower.Contains(pattern, StringComparison.Ordinal)))
                {
                    matches.TryGetValue(skill, out var count);
                    matches[skill] = count + 1;
                }
            }
        }

        return matches
            .Select(kvp => new WeightedSkill
            {
                Name = kvp.Key,
                Weight = CalculateWeight(kvp.Value, isNiceToHave)
            })
            .OrderByDescending(s => s.Weight)
            .ThenBy(s => s.Name)
            .ToList();
    }

    private static double CalculateWeight(int occurrences, bool isNiceToHave)
    {
        if (isNiceToHave)
        {
            return Math.Min(0.6, 0.4 + (occurrences * 0.1));
        }

        return Math.Min(1.0, 0.6 + (occurrences * 0.15));
    }

    private static bool LooksLikeNiceToHave(string line)
    {
        return Regex.IsMatch(
            line,
            @"(nice to have|preferred|preferred skills|bonus|deseable|preferible|plus)",
            RegexOptions.IgnoreCase);
    }

    private static IReadOnlyList<string> ExtractResponsibilities(IEnumerable<string> lines)
    {
        var responsibilities = new List<string>();
        var inSection = false;

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"^(responsibilities|responsabilidades|what you'll do|you will)\b", RegexOptions.IgnoreCase))
            {
                inSection = true;
                continue;
            }

            if (Regex.IsMatch(line, @"^(skills|required|qualifications|requirements)\b", RegexOptions.IgnoreCase))
            {
                inSection = false;
                continue;
            }

            var isBullet = Regex.IsMatch(line, @"^[•*:-]\s+", RegexOptions.IgnoreCase);
            var cleaned = isBullet ? Regex.Replace(line, @"^[•*:-]\s+", string.Empty, RegexOptions.IgnoreCase) : line;

            if (IsSectionHeading(cleaned))
            {
                inSection = false;
                continue;
            }

            if ((inSection && isBullet) || IsResponsibilitySentence(cleaned))
            {
                responsibilities.Add(cleaned);
            }

            if (responsibilities.Count >= 5)
            {
                break;
            }
        }

        return responsibilities
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private static bool IsSectionHeading(string line)
    {
        return Regex.IsMatch(
            line,
            @"^(responsibilities|requirements|qualifications|skills|nice to have|preferred|bonus|about)\b",
            RegexOptions.IgnoreCase);
    }

    private static bool IsResponsibilitySentence(string line)
    {
        return Regex.IsMatch(
            line,
            @"^(design|build|develop|own|maintain|lead|implement|ensure|deliver|improve|optimize)\b",
            RegexOptions.IgnoreCase) && line.Length <= 300;
    }
}
