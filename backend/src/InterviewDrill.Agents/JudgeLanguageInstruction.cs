using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents;

/// <summary>
/// Optional prose-instruction line appended to judge prompts when a session carries an
/// explicit language. Null keeps the historical prompt byte-for-byte so offline tests
/// remain deterministic. Canonical technical keywords always stay in their original form.
/// </summary>
internal static class JudgeLanguageInstruction
{
    public static string? For(string? language)
    {
        return InterviewLanguages.Normalize(language) switch
        {
            InterviewLanguages.Spanish => "Escribe reasoning, strengths y detectedAntiPatterns en español; conserva las palabras clave técnicas canónicas (Polly, HNSW, RAG, Kafka) sin traducir.",
            InterviewLanguages.English => "Write reasoning, strengths and detectedAntiPatterns in English; keep canonical technical keywords unchanged.",
            _ => null
        };
    }
}
