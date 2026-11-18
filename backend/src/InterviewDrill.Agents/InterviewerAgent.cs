using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;

namespace InterviewDrill.Agents;

public sealed class InterviewerAgent(
    IInterviewerQuestionGenerator generator,
    IQuestionRegistry questionRegistry,
    IInterviewerSessionStore sessionStore) : IInterviewerAgent
{
    private const int MaxGenerationAttempts = 3;
    public const int MaxQuestions = 8;

    public async Task<InterviewQuestion> StartSessionAsync(
        string sessionId,
        string userId,
        JobProfile profile,
        CancellationToken ct = default,
        string? language = null)
    {
        var existing = await sessionStore.LoadAsync(sessionId, ct);
        var state = existing ?? Initialize(sessionId, userId, profile);
        state = state with
        {
            UserId = userId,
            Profile = profile,
            JobDescriptionHash = profile.RawTextHash,
            Language = InterviewLanguages.Normalize(language) ?? state.Language
        };

        var skill = InterviewSkillPlanner.SelectFirstSkill(state);
        if (skill is null)
        {
            throw new NoInterviewQuestionsLeftException(sessionId);
        }

        var difficulty = state.CurrentDifficulty;
        var type = GetDefaultQuestionType(profile, skill, difficulty);

        await sessionStore.SaveAsync(state, ct);

        var question = await GenerateAndRegisterAsync(state, skill, difficulty, type, null, ct);

        if (question is null)
        {
            var failures = IncrementValue(state.FailuresBySkill, skill);
            var fallbackSkill = InterviewSkillPlanner.SelectNextSkill(state with { FailuresBySkill = failures }, failures);

            if (fallbackSkill is null || fallbackSkill == skill)
            {
                throw new NoInterviewQuestionsLeftException(sessionId);
            }

            question = await GenerateAndRegisterAsync(
                state,
                fallbackSkill,
                difficulty,
                GetDefaultQuestionType(profile, fallbackSkill, difficulty),
                null,
                ct);

            skill = fallbackSkill;
        }

        if (question is null)
        {
            throw new NoInterviewQuestionsLeftException(sessionId);
        }

        await sessionStore.SaveAsync(
            state with
            {
                CurrentDifficulty = difficulty,
                LastQuestionId = question.Id,
                LastSkillTarget = skill,
                LastQuestionText = question.Text,
                AskedSkillsBySkill = IncrementValue(state.AskedSkillsBySkill, skill)
            },
            ct);

        return question;
    }

    public async Task<InterviewQuestion?> NextQuestionAsync(
        string sessionId,
        string userId,
        EvaluationPayload lastEvaluation,
        CancellationToken ct = default)
    {
        var state = await sessionStore.LoadAsync(sessionId, ct)
            ?? throw new UnknownInterviewSessionException(sessionId);

        if (!string.Equals(state.UserId, userId, StringComparison.Ordinal))
        {
            throw new UnknownInterviewSessionException(sessionId);
        }

        var askedCount = state.AskedSkillsBySkill.Values.Sum();
        if (askedCount >= MaxQuestions)
        {
            await sessionStore.SaveAsync(
                state with
                {
                    LastQuestionId = null,
                    LastSkillTarget = null,
                    LastQuestionText = null
                },
                ct);

            return null;
        }

        var failures = new Dictionary<string, int>(state.FailuresBySkill, StringComparer.OrdinalIgnoreCase);
        var nextDifficulty = DifficultyController.Next(state.CurrentDifficulty, lastEvaluation);
        string? followUpSkill = null;

        if (lastEvaluation.Verdict == Verdict.Fail && state.LastSkillTarget is { Length: > 0 } lastSkill)
        {
            var failCount = IncrementDictionary(failures, lastSkill);

            if (failCount < 2)
            {
                followUpSkill = lastSkill;
            }
        }

        var skill = followUpSkill ?? InterviewSkillPlanner.SelectNextSkill(state, failures);
        if (skill is null)
        {
            await sessionStore.SaveAsync(
                state with
                {
                    CurrentDifficulty = nextDifficulty,
                    FailuresBySkill = failures,
                    LastQuestionId = null,
                    LastSkillTarget = null,
                    LastQuestionText = null
                },
                ct);

            return null;
        }

        var type = followUpSkill is null
            ? GetDefaultQuestionType(state.Profile, skill, nextDifficulty)
            : QuestionType.FollowUp;

        var question = await GenerateAndRegisterAsync(state, skill, nextDifficulty, type, state.LastQuestionText, ct);

        if (question is null)
        {
            IncrementDictionary(failures, skill);
            var fallbackSkill = InterviewSkillPlanner.SelectNextSkill(state with { FailuresBySkill = failures }, failures);

            if (fallbackSkill is null)
            {
                await sessionStore.SaveAsync(
                    state with { CurrentDifficulty = nextDifficulty, FailuresBySkill = failures },
                    ct);

                return null;
            }

            question = await GenerateAndRegisterAsync(
                state,
                fallbackSkill,
                nextDifficulty,
                GetDefaultQuestionType(state.Profile, fallbackSkill, nextDifficulty),
                state.LastQuestionText,
                ct);

            skill = fallbackSkill;
        }

        if (question is null)
        {
            await sessionStore.SaveAsync(
                state with { CurrentDifficulty = nextDifficulty, FailuresBySkill = failures },
                ct);

            return null;
        }

        await sessionStore.SaveAsync(
            state with
            {
                CurrentDifficulty = nextDifficulty,
                FailuresBySkill = failures,
                LastQuestionId = question.Id,
                LastSkillTarget = skill,
                LastQuestionText = question.Text,
                AskedSkillsBySkill = IncrementValue(state.AskedSkillsBySkill, skill)
            },
            ct);

        return question;
    }

    private async Task<InterviewQuestion?> GenerateAndRegisterAsync(
        InterviewerSessionState state,
        string skill,
        Difficulty difficulty,
        QuestionType type,
        string? lastQuestionText,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            var generated = await generator.GenerateAsync(
                new QuestionGenerationRequest(
                    state.SessionId,
                    state.Profile,
                    skill,
                    difficulty,
                    type,
                    lastQuestionText,
                    attempt,
                    state.Language),
                ct);

            var candidate = generated with
            {
                SkillTarget = skill,
                Difficulty = difficulty,
                Type = type
            };

            if (string.IsNullOrWhiteSpace(candidate.Text))
            {
                continue;
            }

            if (await questionRegistry.RegisterAsync(
                    state.SessionId,
                    state.UserId,
                    state.JobDescriptionHash,
                    candidate,
                    ct))
            {
                return candidate;
            }
        }

        return null;
    }

    private static InterviewerSessionState Initialize(string sessionId, string userId, JobProfile profile)
    {
        return new InterviewerSessionState(
            sessionId,
            userId,
            profile.RawTextHash,
            profile,
            DifficultyController.Initial(profile.Seniority),
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
    }

    private static QuestionType GetDefaultQuestionType(JobProfile profile, string skill, Difficulty difficulty)
    {
        if (difficulty == Difficulty.Easy)
        {
            return QuestionType.Conceptual;
        }

        if (difficulty == Difficulty.Medium)
        {
            return QuestionType.Situational;
        }

        return QuestionType.FailureScenario;
    }

    private static IReadOnlyDictionary<string, int> IncrementValue(IReadOnlyDictionary<string, int> source, string key)
    {
        var copy = new Dictionary<string, int>(source, StringComparer.OrdinalIgnoreCase);
        IncrementDictionary(copy, key);
        return copy;
    }

    private static int IncrementDictionary(Dictionary<string, int> source, string key)
    {
        if (!source.TryGetValue(key, out var value))
        {
            value = 0;
        }

        source[key] = value + 1;
        return source[key];
    }
}
