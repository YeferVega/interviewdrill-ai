using System.Text;
using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents;

/// <summary>
/// Deterministic Study Guide Generator.
/// Priority is calculated in C# with formula:
/// priority(topic) = sum(failureFrequency x jdWeight x gap)
/// Markdown prose is localized ("es" default, "en" when the session language is English).
/// </summary>
public sealed class StudyGuideGenerator : IStudyGuideGenerator
{
    private const int MaxTopics = 7;

    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private static readonly (string Name, string[] Keywords)[] Topics =
    [
        ("Resiliencia y protección de tráfico", ["polly", "retry", "retries", "backoff", "rate limit", "429", "circuit breaker", "timeout", "fallback", "degradation", "resilience", "idempotency", "dead letter"]),
        ("Observabilidad y SLOs", ["observability", "opentelemetry", "otel", "activitysource", "distributed tracing", "trace", "tracing", "metrics", "logs", "monitoring", "p95", "p99", "latency"]),
        ("Seguridad de datos y LLM guardrails", ["pii", "privacy", "data masking", "sanitization", "guardrail", "relevance", "human-in-the-loop", "security", "authentication", "authorization", "encryption", "secret", "prompt injection"]),
        ("RAG, embeddings y búsqueda vectorial", ["rag", "retrieval augmented", "retrieval-augmented", "hnsw", "vector search", "vector database", "embeddings", "semantic cache", "reranker"]),
        ("Orquestación con IA y Semantic Kernel", ["semantic kernel", "kernelfunction", "kernel function", "agent orchestration", "orchestration", "zero-math"]),
        ("Streaming y arquitectura orientada a eventos", ["kafka", "event streaming", "pub/sub", "message queue", "rabbitmq", "event-driven"]),
        ("CI/CD y plataformas cloud", ["ci/cd", "cicd", "continuous integration", "continuous delivery", "cloud foundry", "cloudfoundry", "azure", "aws", "deployment", "rollback", "canary"]),
        ("Frontend accesible y rendimiento", ["react", "typescript", "accessibility", "wcag", "lighthouse", "hydration", "bundle"]),
        ("Arquitectura general y trade-offs", ["architecture", "trade-off", "design", "scalability", "availability", "consistency"])
    ];

    private const string DefaultTopic = "Fundamentos backend y arquitectura";

    private static readonly Dictionary<string, string> TopicNamesEn = new(Comparer)
    {
        ["Resiliencia y protección de tráfico"] = "Resilience and traffic management",
        ["Observabilidad y SLOs"] = "Observability and SLOs",
        ["Seguridad de datos y LLM guardrails"] = "Data security and LLM guardrails",
        ["RAG, embeddings y búsqueda vectorial"] = "RAG, embeddings and vector search",
        ["Orquestación con IA y Semantic Kernel"] = "AI orchestration with Semantic Kernel",
        ["Streaming y arquitectura orientada a eventos"] = "Streaming and event-driven architecture",
        ["CI/CD y plataformas cloud"] = "CI/CD and cloud platforms",
        ["Frontend accesible y rendimiento"] = "Accessible frontend and performance",
        ["Arquitectura general y trade-offs"] = "General architecture and trade-offs",
        [DefaultTopic] = "Backend fundamentals and architecture",
    };

    public Task<string> GenerateAsync(
        JobProfile profile,
        IReadOnlyList<EvaluationPayload> sessionEvaluations,
        CancellationToken ct = default,
        string? language = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var en = InterviewLanguages.IsEnglish(language);
        var evaluations = (sessionEvaluations ?? []).Where(x => x is not null).ToList();

        if (evaluations.Count == 0)
        {
            return Task.FromResult(NoDataMarkdown(profile, en));
        }

        var averageScore = Math.Round(evaluations.Average(e => e.OverallScore), 2);
        var missedCounts = CalculateMissedCounts(evaluations);

        if (missedCounts.Count == 0)
        {
            return Task.FromResult(StrongPerformanceMarkdown(profile, evaluations, averageScore, en));
        }

        var topicScores = CalculateTopicScores(profile, missedCounts);
        var maxPriority = topicScores.Count == 0 ? 1 : topicScores.Max(x => x.Priority);
        if (maxPriority <= 0)
        {
            maxPriority = 1;
        }

        var markdown = new StringBuilder();

        markdown.AppendLine(en
            ? $"# Study guide for {profile.Role} ({profile.Seniority})"
            : $"# Guía de estudio para {profile.Role} ({profile.Seniority})");
        markdown.AppendLine();
        markdown.AppendLine(en
            ? "This guide was generated from all the interview evaluations. Each topic priority is calculated in C# with the formula: `failureFrequency × jdWeight × gap`."
            : "Esta guía se generó a partir de todas las evaluaciones de la entrevista. La prioridad de cada tema se calculó en C# con la fórmula: `failureFrequency × jdWeight × gap`.");
        markdown.AppendLine();
        markdown.AppendLine(en
            ? $"- **Interview average:** {averageScore}/100"
            : $"- **Promedio de la entrevista:** {averageScore}/100");
        markdown.AppendLine(en
            ? $"- **Answers evaluated:** {evaluations.Count}"
            : $"- **Respuestas evaluadas:** {evaluations.Count}");
        markdown.AppendLine(en
            ? $"- **Priority topics:** {Math.Min(MaxTopics, topicScores.Count)}"
            : $"- **Temas prioritarios:** {Math.Min(MaxTopics, topicScores.Count)}");
        markdown.AppendLine();

        markdown.AppendLine(en ? "## Demonstrated strengths" : "## Fortalezas demostradas");
        var strengths = CollectTop(evaluations.SelectMany(e => e.Strengths), 5);
        if (strengths.Count == 0)
        {
            markdown.AppendLine(en
                ? "- No strengths were recorded in the evaluations yet."
                : "- Aún no hay fortalezas registradas en las evaluaciones.");
        }
        else
        {
            foreach (var strength in strengths)
            {
                markdown.AppendLine($"- {strength}");
            }
        }

        markdown.AppendLine();
        markdown.AppendLine(en ? "## Prioritized study plan" : "## Plan de estudio priorizado");

        var topics = topicScores
            .OrderByDescending(x => x.Priority)
            .Take(MaxTopics)
            .ToList();

        for (var i = 0; i < topics.Count; i++)
        {
            var topic = topics[i];
            var normalizedPriority = topic.Priority / maxPriority;
            var level = GetPriorityLevel(normalizedPriority, en);
            var minutes = GetMinutes(normalizedPriority);
            var displayName = en ? TopicNamesEn.GetValueOrDefault(topic.Name, topic.Name) : topic.Name;

            markdown.AppendLine();
            markdown.AppendLine($"### {i + 1}. {displayName}");
            markdown.AppendLine(en
                ? $"- **Priority:** {level}"
                : $"- **Prioridad:** {level}");
            markdown.AppendLine(en
                ? $"- **Missed concepts detected:** {string.Join(", ", topic.Concepts)}"
                : $"- **Conceptos fallados detectados:** {string.Join(", ", topic.Concepts)}");
            markdown.AppendLine(en
                ? $"- **Why it matters:** For a {profile.Seniority} profile focused on {profile.Role}, this topic usually decides whether a solution is acceptable or rejected in production."
                : $"- **Por qué importa:** Para un perfil {profile.Seniority} orientado a {profile.Role}, este tema suele decidir si una solución es aceptable o rechazable en producción.");
            markdown.AppendLine(en
                ? $"- **Suggested resources:** {string.Join(" | ", GetResources(topic.Name))}"
                : $"- **Recursos sugeridos:** {string.Join(" | ", GetResources(topic.Name))}");
            markdown.AppendLine(en
                ? $"- **Practice exercise:** {GetExercise(topic.Name, en)}"
                : $"- **Ejercicio de práctica:** {GetExercise(topic.Name, en)}");
            markdown.AppendLine(en
                ? $"- **Estimated time:** {minutes} minutes"
                : $"- **Tiempo estimado:** {minutes} minutos");
        }

        markdown.AppendLine();
        markdown.AppendLine(en ? "## Recommended next step" : "## Siguiente paso recomendado");
        if (en)
        {
            markdown.AppendLine("1. Review the first topic and answer out loud as if it were a real interview.");
            markdown.AppendLine("2. Add production metrics: P95/P99 latency, cost, guardrails, rollback and observability.");
            markdown.AppendLine("3. Repeat this interview to check whether the topic moved from missed to covered.");
        }
        else
        {
            markdown.AppendLine("1. Repasa el primer tema y responde en voz alta como si fuera una entrevista real.");
            markdown.AppendLine("2. Agrega métricas de producción: latencia P95/P99, costo, guardrails, rollback y observabilidad.");
            markdown.AppendLine("3. Repite esta entrevista para verificar si el tema pasó de fallado a cubierto.");
        }

        return Task.FromResult(markdown.ToString());
    }

    private static string NoDataMarkdown(JobProfile profile, bool en)
    {
        return en
            ? $"""
                # Study guide for {profile.Role} ({profile.Seniority})

                There are not enough evaluations yet to generate a prioritized guide.
                Complete an interview so the system can detect topics to reinforce.
                """
            : $"""
                # Guía de estudio para {profile.Role} ({profile.Seniority})

                Aún no hay evaluaciones suficientes para generar una guía priorizada.
                Completa una entrevista para que el sistema pueda detectar temas a reforzar.
                """;
    }

    private static string StrongPerformanceMarkdown(
        JobProfile profile,
        IReadOnlyList<EvaluationPayload> evaluations,
        double averageScore,
        bool en)
    {
        var strengths = CollectTop(evaluations.SelectMany(e => e.Strengths), 5);
        var markdown = new StringBuilder();

        markdown.AppendLine(en
            ? $"# Study guide for {profile.Role} ({profile.Seniority})"
            : $"# Guía de estudio para {profile.Role} ({profile.Seniority})");
        markdown.AppendLine();
        markdown.AppendLine(en
            ? $"High average: **{averageScore}/100**. No recurring missed topics were detected."
            : $"Promedio alto: **{averageScore}/100**. No se detectaron temas fallados recurrentes.");
        markdown.AppendLine();
        markdown.AppendLine(en ? "## Strengths" : "## Fortalezas");

        if (strengths.Count == 0)
        {
            markdown.AppendLine(en
                ? "- Good overall level detected in the interview."
                : "- Buen nivel general detectado en la entrevista.");
        }
        else
        {
            foreach (var strength in strengths)
            {
                markdown.AppendLine($"- {strength}");
            }
        }

        markdown.AppendLine();
        markdown.AppendLine(en ? "## Next level" : "## Siguiente nivel");
        if (en)
        {
            markdown.AppendLine("- Practice production scenarios with quantifiable metrics.");
            markdown.AppendLine("- Go deeper into distributed design, costs and rollback.");
            markdown.AppendLine("- Repeat the interview to keep getting fresh questions.");
        }
        else
        {
            markdown.AppendLine("- Practica escenarios de producción con métricas cuantificables.");
            markdown.AppendLine("- Profundiza en diseño distribuido, costos y rollback.");
            markdown.AppendLine("- Repite la entrevista para conservar preguntas nuevas.");
        }

        return markdown.ToString();
    }

    private static Dictionary<string, List<double>> CalculateMissedCounts(IReadOnlyList<EvaluationPayload> evaluations)
    {
        var missedScores = new Dictionary<string, List<double>>(Comparer);

        foreach (var evaluation in evaluations)
        {
            foreach (var concept in Distinct(evaluation.MissedConcepts))
            {
                if (!missedScores.TryGetValue(concept, out var scores))
                {
                    scores = [];
                    missedScores[concept] = scores;
                }

                scores.Add(evaluation.OverallScore);
            }
        }

        return missedScores;
    }

    private static IReadOnlyList<TopicScore> CalculateTopicScores(
        JobProfile profile,
        Dictionary<string, List<double>> missedScores)
    {
        var topicMap = new Dictionary<string, TopicAccumulator>(Comparer);

        foreach (var pair in missedScores)
        {
            var topicName = FindTopic(pair.Key);
            var frequency = pair.Value.Count;
            var weight = GetWeight(profile, pair.Key, topicName);
            var averageScore = pair.Value.Average();
            var gap = Math.Clamp(1 - (averageScore / 100), 0, 1);
            var priority = frequency * weight * gap;

            if (!topicMap.TryGetValue(topicName, out var accumulator))
            {
                accumulator = new TopicAccumulator(topicName);
                topicMap[topicName] = accumulator;
            }

            accumulator.Priority += priority;
            accumulator.Concepts.Add(pair.Key);
        }

        return topicMap.Values
            .Select(x => new TopicScore(x.Name, x.Priority, x.Concepts.Distinct(Comparer).ToList()))
            .ToList();
    }

    private static double GetWeight(JobProfile profile, string concept, string topicName)
    {
        var requiredSkill = profile.RequiredSkills
            .FirstOrDefault(skill => skill.Name.Contains(concept, StringComparison.OrdinalIgnoreCase)
                || concept.Contains(skill.Name, StringComparison.OrdinalIgnoreCase));

        if (requiredSkill is not null)
        {
            return requiredSkill.Weight;
        }

        var topicSkill = profile.RequiredSkills
            .FirstOrDefault(skill => skill.Name.Contains(topicName, StringComparison.OrdinalIgnoreCase));

        return topicSkill?.Weight ?? 0.5;
    }

    private static string FindTopic(string concept)
    {
        var normalized = concept.ToLowerInvariant();

        foreach (var topic in Topics)
        {
            if (topic.Keywords.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal)))
            {
                return topic.Name;
            }
        }

        return DefaultTopic;
    }

    private static string GetPriorityLevel(double priority, bool en)
    {
        if (priority >= 0.8)
        {
            return en ? "High" : "Alta";
        }

        if (priority >= 0.5)
        {
            return en ? "Medium" : "Media";
        }

        return en ? "Low" : "Baja";
    }

    private static int GetMinutes(double priority)
    {
        if (priority >= 0.8)
        {
            return 90;
        }

        if (priority >= 0.5)
        {
            return 60;
        }

        return 40;
    }

    private static IEnumerable<string> GetResources(string topicName)
    {
        // Resource names are documentation titles: language-neutral in both outputs.
        return topicName switch
        {
            "RAG, embeddings y búsqueda vectorial" =>
                ["Semantic Kernel docs", "Microsoft vector search docs", "Qdrant/Pinecone/Weaviate vector search docs", "HNSW paper or vector index docs"],
            "Resiliencia y protección de tráfico" =>
                ["Polly docs", "Azure Architecture Center", "Kafka docs", "AWS/Google resilience patterns"],
            "Observabilidad y SLOs" =>
                ["OpenTelemetry docs", ".NET distributed tracing docs", "Microsoft monitoring/SRE guides"],
            "Seguridad de datos y LLM guardrails" =>
                ["OWASP LLM Top 10", "NIST AI Risk Management Framework", "Azure OpenAI safety docs", "PII/data masking docs"],
            "Orquestación con IA y Semantic Kernel" =>
                ["Semantic Kernel docs", "Kernel Function examples", "Microsoft Agent orchestration docs"],
            "Streaming y arquitectura orientada a eventos" =>
                ["Apache Kafka docs", "Event-driven architecture guides", "Azure/Amazon eventing docs"],
            "CI/CD y plataformas cloud" =>
                ["GitHub Actions docs", "Azure DevOps docs", "AWS CodePipeline docs", "Cloud Foundry docs"],
            "Frontend accesible y rendimiento" =>
                ["React accessibility docs", "WCAG 2.2 quickref", "web.dev performance patterns"],
            _ =>
                ["Microsoft Architecture Center", "AWS Well-Architected", "SRE handbook"]
        };
    }

    private static string GetExercise(string topicName, bool en)
    {
        if (en)
        {
            return topicName switch
            {
                "RAG, embeddings y búsqueda vectorial" =>
                    "Design a RAG pipeline with chunking, embeddings, HNSW, a reranker and a relevance guardrail. Define how you would measure accuracy, cost and latency.",
                "Resiliencia y protección de tráfico" =>
                    "Implement a plan for HTTP 429 with retry backoff, jitter, a circuit breaker and a reprocessing queue. Explain what happens when the LLM provider goes down.",
                "Observabilidad y SLOs" =>
                    "Add tracing, P95/P99 latency metrics, structured logs and alerts for an LLM call. Include a troubleshooting dashboard.",
                "Seguridad de datos y LLM guardrails" =>
                    "Create a design with PII masking before inference, prompt validation, relevance thresholds and human review for sensitive actions.",
                "Orquestación con IA y Semantic Kernel" =>
                    "Redesign an LLM integration with KernelFunctions, plan/step functions and provider abstraction to avoid direct coupling.",
                "Streaming y arquitectura orientada a eventos" =>
                    "Model an asynchronous flow with Kafka for ingestion and LLM evaluation. Include ordering, idempotency, DLQ and monitoring.",
                "CI/CD y plataformas cloud" =>
                    "Design a pipeline with tests, quality gates, canary deployment, automatic rollback and security checks.",
                "Frontend accesible y rendimiento" =>
                    "Optimize an interview UI with keyboard navigation, aria-live, lazy loading and Core Web Vitals metrics.",
                _ =>
                    "Document an interview scenario with trade-offs, metrics and production mitigations."
            };
        }

        return topicName switch
        {
            "RAG, embeddings y búsqueda vectorial" =>
                "Diseña un pipeline RAG con chunking, embeddings, HNSW, reranker y guardrail de relevancia. Define cómo medirías precisión, costo y latencia.",
            "Resiliencia y protección de tráfico" =>
                "Implementa un plan para HTTP 429 con retry con backoff, jitter, circuit breaker y cola de reprocesamiento. Explica qué ocurre si el proveedor LLM se cae.",
            "Observabilidad y SLOs" =>
                "Agrega tracing, métricas de latencia P95/P99, logs estructurados y alertas para una llamada LLM. Incluye un dashboard de troubleshooting.",
            "Seguridad de datos y LLM guardrails" =>
                "Crea un diseño con PII masking antes de inferencia, validación de prompts, thresholds de relevancia y revisión humana para acciones sensibles.",
            "Orquestación con IA y Semantic Kernel" =>
                "Rediseña una integración LLM con KernelFunctions, plan/step functions y abstraction de proveedor para evitar acoplamiento directo.",
            "Streaming y arquitectura orientada a eventos" =>
                "Modela un flujo asíncrono con Kafka para ingestión y evaluación LLM. Incluye ordering, idempotencia, DLQ y monitoreo.",
            "CI/CD y plataformas cloud" =>
                "Diseña un pipeline con tests, quality gates, deploy canary, rollback automático y checks de seguridad.",
            "Frontend accesible y rendimiento" =>
                "Optimiza una interfaz de entrevista con navegación por teclado, aria-live, lazy loading y métricas Core Web Vitals.",
            _ =>
                "Documenta un escenario de entrevista con trade-offs, métricas y mitigaciones de producción."
        };
    }

    private static IReadOnlyList<string> CollectTop(IEnumerable<string> values, int max)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, Comparer)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, Comparer)
            .Select(group => group.Key)
            .Take(max)
            .ToList();
    }

    private static IReadOnlyList<string> Distinct(IEnumerable<string> values)
    {
        return values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(Comparer).ToList();
    }

    private sealed record TopicScore(string Name, double Priority, IReadOnlyList<string> Concepts);

    private sealed class TopicAccumulator(string name)
    {
        public string Name { get; } = name;
        public double Priority { get; set; }
        public List<string> Concepts { get; } = [];
    }
}
