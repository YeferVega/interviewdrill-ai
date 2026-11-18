using InterviewDrill.Agents;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Security;
using Xunit;

namespace InterviewDrill.Agents.Tests;

/// <summary>
/// Fake <see cref="ILlmCompletionClient"/> que captura el último <see cref="LlmCompletionRequest.UserPrompt"/>
/// para verificar que la PII se sanitiza antes de enviar al LLM.
/// </summary>
public sealed class CapturingLlmClient : ILlmCompletionClient
{
    public string? LastUserPrompt { get; private set; }
    public string? LastSystemPrompt { get; private set; }
    public string ResponseToReturn { get; set; } = "{}";

    public Task<string> CompleteAsync(LlmCompletionRequest request, CancellationToken ct = default)
    {
        LastSystemPrompt = request.SystemPrompt;
        LastUserPrompt = request.UserPrompt;
        return Task.FromResult(ResponseToReturn);
    }
}

/// <summary>
/// Fake <see cref="IPiiSanitizer"/> que enmascara emails y teléfonos para probar
/// que los agentes sanitizan PII antes de enviar al LLM.
/// </summary>
public sealed class MaskingPiiSanitizer : IPiiSanitizer
{
    public PiiSanitizationResult Sanitize(string? input, bool mask = true)
    {
        var text = input ?? string.Empty;
        var findings = new List<PiiFinding>();

        if (mask)
        {
            var emailMatch = System.Text.RegularExpressions.Regex.Match(text, @"[\w\.-]+@[\w\.-]+\.\w+");
            if (emailMatch.Success)
            {
                text = text.Replace(emailMatch.Value, "[EMAIL]");
                findings.Add(new PiiFinding("Email", "[EMAIL]", emailMatch.Index, emailMatch.Length, 100));
            }

            var phoneMatch = System.Text.RegularExpressions.Regex.Match(text, @"\+?\d[\d\s\-]{7,}");
            if (phoneMatch.Success)
            {
                text = text.Replace(phoneMatch.Value, "[PHONE]");
                findings.Add(new PiiFinding("Phone", "[PHONE]", phoneMatch.Index, phoneMatch.Length, 90));
            }
        }

        return new PiiSanitizationResult(text, findings);
    }

    public void AddPattern(PiiPatternDefinition definition) { }
}

public class LlmPiiIntegrationTests
{
    private static readonly CandidateResponse SampleResponse = new()
    {
        SessionId = "session-1",
        QuestionId = "q-1",
        Text = "",
        SkillTarget = "Resiliencia"
    };

    [Fact]
    public async Task LlmArchitectureJudge_SanitizesPiiBeforeSendingToLlm()
    {
        var llm = new CapturingLlmClient
        {
            ResponseToReturn = """{"reasoning":"ok","clarity":4,"technicalAccuracy":4,"architectureDepth":4,"communication":4,"coveredKeywords":[],"missedConcepts":[],"strengths":[],"detectedAntiPatterns":[]}"""
        };
        var sanitizer = new MaskingPiiSanitizer();
        var judge = new LlmArchitectureJudge(llm, sanitizer);

        var preFilter = new ArchitectureEvaluation
        {
            TechnicalDepth = 0.8,
            CoveredKeywords = [],
            MissedConcepts = [],
            Strengths = []
        };

        var response = SampleResponse with
        {
            Text = "Mi email es alice@example.com y mi teléfono es +1 555-123-4567. Usamos Polly para resiliencia."
        };

        await judge.JudgeAsync(response, preFilter);

        var prompt = llm.LastUserPrompt ?? string.Empty;
        // La PII del input original NO debe aparecer en el prompt enviado al LLM
        Assert.DoesNotContain("alice@example.com", prompt);
        Assert.DoesNotContain("+1 555-123-4567", prompt);
        // Los placeholders deben estar presentes
        Assert.Contains("[EMAIL]", prompt);
        Assert.Contains("[PHONE]", prompt);
        // El contenido no-PII sí debe estar presente
        Assert.Contains("Polly", prompt);
    }

    [Fact]
    public async Task LlmSecurityJudge_SanitizesPiiBeforeSendingToLlm()
    {
        var llm = new CapturingLlmClient
        {
            ResponseToReturn = """{"reasoning":"ok","piiProtection":5,"relevanceGuardrails":5,"observability":4,"resilience":4,"deploymentSafety":4,"coveredKeywords":[],"missedConcepts":[],"strengths":[],"securityConcerns":[]}"""
        };
        var sanitizer = new MaskingPiiSanitizer();
        var judge = new LlmSecurityJudge(llm, sanitizer);

        var preFilter = new SecurityEvaluation
        {
            ProductionReadiness = 0.8,
            CoveredKeywords = [],
            MissedConcepts = [],
            Strengths = [],
            ResilienceScore = 0.8
        };

        var response = SampleResponse with
        {
            Text = "Mi email es alice@example.com y mi teléfono es +1 555-123-4567. Usamos circuit breaker para resiliencia."
        };

        await judge.JudgeAsync(response, preFilter);

        var prompt = llm.LastUserPrompt ?? string.Empty;
        Assert.DoesNotContain("alice@example.com", prompt);
        Assert.DoesNotContain("+1 555-123-4567", prompt);
        Assert.Contains("[EMAIL]", prompt);
        Assert.Contains("[PHONE]", prompt);
        Assert.Contains("circuit breaker", prompt);
    }

    [Fact]
    public async Task LlmInterviewerQuestionGenerator_SanitizesPiiBeforeSendingToLlm()
    {
        var llm = new CapturingLlmClient { ResponseToReturn = """{"text":"¿Cómo manejas fallos en producción?","type":"failure-scenario"}""" };
        var sanitizer = new MaskingPiiSanitizer();
        var generator = new LlmInterviewerQuestionGenerator(llm, sanitizer);

        var request = new QuestionGenerationRequest(
            "session-1",
            new JobProfile
            {
                Role = "Backend Developer",
                Seniority = Seniority.Mid,
                RequiredSkills = [],
                NiceToHaveSkills = [],
                Responsibilities = ["Gestionar credenciales de alice@example.com en producción"],
                RawTextHash = "hash"
            },
            "Seguridad",
            Difficulty.Medium,
            QuestionType.FailureScenario,
            null,
            1);

        await generator.GenerateAsync(request);

        var prompt = llm.LastUserPrompt ?? string.Empty;
        Assert.DoesNotContain("alice@example.com", prompt);
        Assert.Contains("[EMAIL]", prompt);
    }

    [Fact]
    public async Task MaskingPiiSanitizer_MasksEmailAndPhone()
    {
        var sanitizer = new MaskingPiiSanitizer();
        var jd = "Buscamos desarrollador. Contacto: alice@example.com, teléfono +1 555-123-4567.";

        var result = sanitizer.Sanitize(jd);

        Assert.DoesNotContain("alice@example.com", result.Text);
        Assert.DoesNotContain("+1 555-123-4567", result.Text);
        Assert.Contains("[EMAIL]", result.Text);
        Assert.Contains("[PHONE]", result.Text);
    }
}
