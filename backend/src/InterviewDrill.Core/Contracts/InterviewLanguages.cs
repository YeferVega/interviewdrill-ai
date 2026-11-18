namespace InterviewDrill.Core.Contracts;

/// <summary>
/// Códigos canónicos de idioma para entrevistas bilingues ("es" | "en").
/// Solo los textos libres (preguntas, consejos, guías) se localizan; los puntajes,
/// veredictos y keywords técnicas canónicas permanecen neutrales de idioma, y el
/// contrato EvaluationPayload del constitution no cambia.
/// </summary>
public static class InterviewLanguages
{
    public const string Spanish = "es";
    public const string English = "en";

    /// <summary>Normaliza aliases (ES/es-ES/spanish/EN/en-US...). Devuelve null si no se reconoce.</summary>
    public static string? Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "es" or "es-es" or "es-mx" or "español" or "espanol" or "spanish" => Spanish,
            "en" or "en-us" or "en-gb" or "english" => English,
            _ => null
        };
    }

    public static bool IsSpanish(string? value) => Normalize(value) == Spanish;

    public static bool IsEnglish(string? value) => Normalize(value) == English;
}
