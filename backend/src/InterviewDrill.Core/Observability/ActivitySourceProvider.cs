using System.Diagnostics;

namespace InterviewDrill.Core.Observability;

/// <summary>
/// Proveedor centralizado de ActivitySource para tracing distribuido con OpenTelemetry.
/// Todas las actividades de la aplicación usan este nombre para correlacionar traces
/// entre llamadas LLM, consultas a base de datos y operaciones de sesión.
/// </summary>
public static class ActivitySourceProvider
{
    public const string ActivitySourceName = "InterviewDrill";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static readonly ActivitySource LlmActivitySource = new($"{ActivitySourceName}.Llm");
    public static readonly ActivitySource DbActivitySource = new($"{ActivitySourceName}.Db");
    public static readonly ActivitySource SessionActivitySource = new($"{ActivitySourceName}.Session");
    public static readonly ActivitySource EvaluationActivitySource = new($"{ActivitySourceName}.Evaluation");
}
