using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;

namespace InterviewDrill.Agents;

public sealed class HeuristicInterviewerQuestionGenerator : IInterviewerQuestionGenerator
{
    private static readonly string[] ConceptualTemplatesEs =
    [
        "Explica qué es {skill} y cuándo sería útil para un {role}.",
        "Describe con tus palabras la responsabilidad principal de {skill} en una arquitectura {seniority}.",
        "¿Qué patrón de diseño asociarías con {skill} y por qué?"
    ];

    private static readonly string[] SituationalTemplatesEs =
    [
        "Diseña una solución usando {skill} para: {responsibility}.",
        "¿Cómo integrarías {skill} en un pipeline existente sin bloquear la experiencia del usuario?",
        "Un equipo necesita usar {skill} para mejorar {responsibility}. ¿Qué propuesta harías?"
    ];

    private static readonly string[] FailureScenarioTemplatesEs =
    [
        "En producción, {skill} empieza a causar errores intermitentes bajo carga. ¿Cómo diagnosticas el problema y reduces la latencia P95?",
        "Un servicio dependiente de {skill} devuelve HTTP 429 durante picos de tráfico. ¿Cómo diseñas resiliencia, retries y degradación controlada?",
        "Detectas que {skill} puede filtrar datos sensibles. ¿Cómo previenes la fuga sin detener la operación?"
    ];

    private static readonly string[] ExpertTemplatesEs =
    [
        "Diseña la estrategia de {skill} para un servicio con cumplimiento, privacidad y control de costos. Incluye KPIs, guardrails y plan de rollback.",
        "¿Cómo operarías {skill} a escala multi-región minimizando riesgo, costo y deuda técnica?",
        "Evalúa las compensaciones entre consistencia, disponibilidad y latencia al usar {skill} en producción crítica."
    ];

    private static readonly string[] FollowUpTemplatesEs =
    [
        "Profundiza en {skill}: ¿qué métricas presentarías para demostrar que tu propuesta funciona?",
        "Volvamos a {skill}: ¿qué antipatrón evitarías específicamente y por qué?",
        "¿Cómo probarías en producción el diseño de {skill} que acabas de describir?"
    ];

    private static readonly string[] ConceptualTemplatesEn =
    [
        "Explain what {skill} is and when it would be useful for a {role}.",
        "Describe in your own words the main responsibility of {skill} in a {seniority} architecture.",
        "Which design pattern would you associate with {skill} and why?"
    ];

    private static readonly string[] SituationalTemplatesEn =
    [
        "Design a solution using {skill} for: {responsibility}.",
        "How would you integrate {skill} into an existing pipeline without blocking the user experience?",
        "A team needs to use {skill} to improve {responsibility}. What would you propose?"
    ];

    private static readonly string[] FailureScenarioTemplatesEn =
    [
        "In production, {skill} starts causing intermittent errors under load. How do you diagnose the issue and reduce P95 latency?",
        "A service that depends on {skill} returns HTTP 429 during traffic spikes. How do you design resilience, retries and controlled degradation?",
        "You detect that {skill} could leak sensitive data. How do you prevent the leak without stopping operations?"
    ];

    private static readonly string[] ExpertTemplatesEn =
    [
        "Design the {skill} strategy for a service with compliance, privacy and cost control. Include KPIs, guardrails and a rollback plan.",
        "How would you operate {skill} at multi-region scale while minimizing risk, cost and technical debt?",
        "Evaluate the trade-offs between consistency, availability and latency when using {skill} in critical production."
    ];

    private static readonly string[] FollowUpTemplatesEn =
    [
        "Go deeper on {skill}: which metrics would you present to prove your proposal works?",
        "Back to {skill}: which anti-pattern would you specifically avoid and why?",
        "How would you validate in production the {skill} design you just described?"
    ];

    public Task<InterviewQuestion> GenerateAsync(QuestionGenerationRequest request, CancellationToken ct = default)
    {
        // Default (null) stays Spanish to preserve historical behavior; "en" switches the bank.
        var english = InterviewLanguages.IsEnglish(request.Language);

        var skill = Normalize(request.SkillTarget);
        var role = Normalize(request.Profile.Role);
        var seniority = request.Profile.Seniority.ToString();
        var responsibility = Normalize(
            request.Profile.Responsibilities.FirstOrDefault()
            ?? (english ? "improve the user experience" : "mejorar la experiencia de usuario"));

        var templates = (english, request.Type, request.Difficulty) switch
        {
            (true, QuestionType.Conceptual, _) => ConceptualTemplatesEn,
            (true, QuestionType.FailureScenario, Difficulty.Expert) => ExpertTemplatesEn,
            (true, QuestionType.FailureScenario, _) => FailureScenarioTemplatesEn,
            (true, QuestionType.FollowUp, _) => FollowUpTemplatesEn,
            (true, _, _) => SituationalTemplatesEn,
            (false, QuestionType.Conceptual, _) => ConceptualTemplatesEs,
            (false, QuestionType.FailureScenario, Difficulty.Expert) => ExpertTemplatesEs,
            (false, QuestionType.FailureScenario, _) => FailureScenarioTemplatesEs,
            (false, QuestionType.FollowUp, _) => FollowUpTemplatesEs,
            (false, _, _) => SituationalTemplatesEs
        };

        var template = templates[request.Attempt % templates.Length];

        var text = template
            .Replace("{skill}", skill, StringComparison.Ordinal)
            .Replace("{role}", role, StringComparison.Ordinal)
            .Replace("{seniority}", seniority, StringComparison.Ordinal)
            .Replace("{responsibility}", responsibility, StringComparison.Ordinal);

        return Task.FromResult(new InterviewQuestion
        {
            Id = Guid.NewGuid().ToString(),
            Text = text,
            SkillTarget = request.SkillTarget,
            Difficulty = request.Difficulty,
            Type = request.Type
        });
    }

    private static string Normalize(string value)
    {
        return value.Trim().TrimEnd('.', '?', '!');
    }
}
