namespace InterviewDrill.Core.Contracts;

/// <summary>Parses a Job Description document into a structured JobProfile.</summary>
public interface IJdParserAgent
{
    /// <param name="content">Extracted text (via MarkItDown) or raw pasted text.</param>
    Task<JobProfile> ParseAsync(string content, CancellationToken ct = default);
}

/// <summary>Conducts the interview: generates adaptive, non-repeated questions.</summary>
public interface IInterviewerAgent
{
    /// <summary>Optional language ("es"/"en") goes AFTER ct so existing positional callers never bind it by mistake.</summary>
    Task<InterviewQuestion> StartSessionAsync(
        string sessionId,
        string userId,
        JobProfile profile,
        CancellationToken ct = default,
        string? language = null);

    /// <summary>Applies the evaluation feedback, adjusts difficulty/skill selection and returns the next question.</summary>
    Task<InterviewQuestion?> NextQuestionAsync(string sessionId, string userId, EvaluationPayload lastEvaluation, CancellationToken ct = default);
}

/// <summary>Assesses technical depth (0-100) with hybrid deterministic + LLM-judge evaluation.</summary>
public interface IArchitectureEvaluator
{
    Task<ArchitectureEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default);
}

/// <summary>Assesses production readiness (0-100): security, guardrails, resilience.</summary>
public interface ISecurityRigorJudge
{
    Task<SecurityEvaluation> EvaluateAsync(CandidateResponse response, CancellationToken ct = default);
}

/// <summary>Consolidates both evaluations into the final EvaluationPayload (weighted score + verdict).</summary>
public interface IScoringSynthesizer
{
    Task<EvaluationPayload> SynthesizeAsync(
        ArchitectureEvaluation architectureEval,
        SecurityEvaluation securityEval,
        CancellationToken ct = default,
        string? language = null);
}

/// <summary>Generates the prioritized study guide at interview end.</summary>
public interface IStudyGuideGenerator
{
    Task<string> GenerateAsync(
        JobProfile profile,
        IReadOnlyList<EvaluationPayload> sessionEvaluations,
        CancellationToken ct = default,
        string? language = null);
}

/// <summary>Persistent question registry enforcing the no-repeat guarantee (exact + semantic).</summary>
public interface IQuestionRegistry
{
    /// <summary>Returns true if the question (or a near-duplicate) was already asked for this user+JD.</summary>
    Task<bool> WasAlreadyAskedAsync(string userId, string jdHash, string questionText, CancellationToken ct = default);

    /// <summary>Registers a newly asked question. Returns false if an exact duplicate was already present.</summary>
    Task<bool> RegisterAsync(string sessionId, string userId, string jdHash, InterviewQuestion question, CancellationToken ct = default);
}
