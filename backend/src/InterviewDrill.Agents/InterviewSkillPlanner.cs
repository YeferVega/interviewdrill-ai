using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;

namespace InterviewDrill.Agents;

public sealed record SkillCandidate(string Name, double Weight, int AskedCount, int FailureCount);

public static class InterviewSkillPlanner
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;
    private const int ExhaustedFailureCount = 2;

    public static string? SelectFirstSkill(InterviewerSessionState state)
    {
        return SelectSkill(state.Profile, state.FailuresBySkill, state.AskedSkillsBySkill);
    }

    public static string? SelectNextSkill(InterviewerSessionState state, IReadOnlyDictionary<string, int> failures)
    {
        return SelectSkill(state.Profile, failures, state.AskedSkillsBySkill);
    }

    private static string? SelectSkill(
        JobProfile profile,
        IReadOnlyDictionary<string, int> failures,
        IReadOnlyDictionary<string, int> askedCounts)
    {
        var candidates = BuildCandidates(profile, failures, askedCounts)
            .Where(c => c.FailureCount < ExhaustedFailureCount)
            .OrderBy(c => c.AskedCount)
            .ThenByDescending(c => c.Weight)
            .ThenBy(c => c.Name, Comparer)
            .ToList();

        if (candidates.Count > 0)
        {
            return candidates[0].Name;
        }

        // If every skill/responsibility has been exhausted, select the least asked fallback anyway.
        // The InterviewerAgent will still attempt deduplication before accepting the generated question.
        return BuildCandidates(profile, failures, askedCounts)
            .OrderBy(c => c.AskedCount)
            .ThenByDescending(c => c.Weight)
            .ThenBy(c => c.Name, Comparer)
            .Select(c => c.Name)
            .FirstOrDefault();
    }

    private static IEnumerable<SkillCandidate> BuildCandidates(
        JobProfile profile,
        IReadOnlyDictionary<string, int> failures,
        IReadOnlyDictionary<string, int> askedCounts)
    {
        foreach (var skill in profile.RequiredSkills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name))
            {
                continue;
            }

            yield return new SkillCandidate(
                skill.Name,
                skill.Weight,
                GetCount(askedCounts, skill.Name),
                GetCount(failures, skill.Name));
        }

        foreach (var responsibility in profile.Responsibilities)
        {
            if (string.IsNullOrWhiteSpace(responsibility))
            {
                continue;
            }

            yield return new SkillCandidate(
                responsibility,
                0.45,
                GetCount(askedCounts, responsibility),
                GetCount(failures, responsibility));
        }

        foreach (var niceToHave in profile.NiceToHaveSkills)
        {
            if (string.IsNullOrWhiteSpace(niceToHave))
            {
                continue;
            }

            yield return new SkillCandidate(
                niceToHave,
                0.35,
                GetCount(askedCounts, niceToHave),
                GetCount(failures, niceToHave));
        }

        yield return new SkillCandidate(
            $"Producción de {profile.Role}",
            0.3,
            GetCount(askedCounts, $"Producción de {profile.Role}"),
            GetCount(failures, $"Producción de {profile.Role}"));
    }

    private static int GetCount(IReadOnlyDictionary<string, int> source, string key)
    {
        foreach (var pair in source)
        {
            if (Comparer.Equals(pair.Key, key))
            {
                return pair.Value;
            }
        }

        return 0;
    }
}
