using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using InterviewDrill.Agents;
using InterviewDrill.Api;
using InterviewDrill.Core.Observability;
using InterviewDrill.Core.Caching;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Documents;
using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Core.Llm;
using InterviewDrill.Core.Pdf;
using InterviewDrill.Core.Persistence;
using InterviewDrill.Core.Scoring;
using InterviewDrill.Core.Security;
using InterviewDrill.Infrastructure;
using InterviewDrill.Infrastructure.Caching;
using InterviewDrill.Infrastructure.Documents;
using InterviewDrill.Infrastructure.Llm;
using InterviewDrill.Infrastructure.Logging;
using InterviewDrill.Infrastructure.Pdf;
using InterviewDrill.Infrastructure.Persistence;
using InterviewDrill.Infrastructure.Resilience;
using InterviewDrill.Infrastructure.Security;
using Microsoft.Extensions.Logging.Console;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

var builder = WebApplication.CreateBuilder(args);

// Configuración de OpenTelemetry con ActivitySource para tracing distribuido
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(ActivitySourceProvider.ActivitySourceName)
        .AddSource(ActivitySourceProvider.LlmActivitySource.Name)
        .AddSource(ActivitySourceProvider.DbActivitySource.Name)
        .AddSource(ActivitySourceProvider.SessionActivitySource.Name)
        .AddSource(ActivitySourceProvider.EvaluationActivitySource.Name)
        .AddConsoleExporter());

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = JdDocumentTextExtractor.MaxBytes;
});

builder.Services.AddSingleton<IPiiSanitizer, RegexPiiSanitizer>();

// Logging estructurado sin PII ni secretos: envuelve el proveedor de consola para
// sanitizar PII y credenciales antes de escribir a cualquier sink de log.
// Instancia directa (sin BuildServiceProvider): mismo objeto registrado en el contenedor.
var piiSanitizer = new RegexPiiSanitizer();
builder.Services.AddSingleton(piiSanitizer);
builder.Logging.AddProvider(new PiiSanitizingLoggerProvider(piiSanitizer, new ConsoleLoggerProvider(new SimpleOptionsMonitor<ConsoleLoggerOptions>(new ConsoleLoggerOptions()))));
builder.Services.AddSingleton<IJdDocumentTextExtractor, JdDocumentTextExtractor>();

// Configuración de proveedores LLM (Azure OpenAI / Azure Foundry, Bedrock, Ollama)
builder.Services.AddOptions<LlmProviderOptions>().Bind(builder.Configuration.GetSection("LLM"));

var llmProvider = (builder.Configuration["LLM:Provider"] ?? "Heuristic").Trim();

switch (llmProvider)
{
    // NVIDIA NIM y OpenAI comparten el esquema /chat/completions (OpenAI-compatible).
    case "OpenAICompatible":
    case "Nvidia":
    case "NVIDIA":
    case "OpenAI":
    case "OpenCode":
        builder.Services.AddHttpClient("openai-compat", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(PollyPolicies.HttpTimeoutSeconds);
        });
        builder.Services.AddSingleton<ILlmCompletionClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LlmProviderOptions>>().Value.OpenAiCompatible ?? new OpenAiCompatibleOptions();

            // Fallback de credenciales por variables de entorno estandarizadas de cada proveedor.
            // La Constitución prohíbe hardcodear secretos: la API key SIEMPRE debe venir por
            // config o por variable de entorno.
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                if (options.BaseUrl?.Contains("integrate.api.nvidia.com", StringComparison.OrdinalIgnoreCase) == true)
                    options.ApiKey = Environment.GetEnvironmentVariable("NVIDIA_API_KEY");
                else if (options.BaseUrl?.Contains("opencode.ai", StringComparison.OrdinalIgnoreCase) == true)
                    options.ApiKey = Environment.GetEnvironmentVariable("OPENCODE_API_KEY");
                else if (options.BaseUrl?.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase) == true)
                    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }

            // Si seguimos sin API key, el proveedor no se puede usar: usamos el cliente
            // Unavailable para que los agentes caigan al modo heurístico determinista.
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                return new UnavailableLlmCompletionClient();

            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("openai-compat");
            return new OpenAiCompatibleLlmClient(http, options);
        });
        break;

    case "AzureOpenAI":
        builder.Services.AddHttpClient<IJdParserAgent, LlmJdParserAgent>(client =>
        {
            var options = builder.Configuration.GetSection("LLM:AzureOpenAI").Get<AzureOpenAIOptions>() ?? new AzureOpenAIOptions();
            client.BaseAddress = new Uri(options?.Endpoint ?? "https://placeholder.openai.azure.com/");
            client.Timeout = TimeSpan.FromSeconds(PollyPolicies.HttpTimeoutSeconds);
        });
        builder.Services.AddSingleton<ILlmCompletionClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LlmProviderOptions>>().Value.AzureOpenAI ?? new AzureOpenAIOptions();
            return new AzureOpenAILlmClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(), options);
        });
        break;

    case "Bedrock":
        builder.Services.AddHttpClient<ILlmCompletionClient, BedrockLlmClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(PollyPolicies.HttpTimeoutSeconds);
        });
        break;

    case "Ollama":
        builder.Services.AddHttpClient<ILlmCompletionClient, OllamaLlmClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LlmProviderOptions>>().Value.Ollama ?? new OllamaOptions();
            client.BaseAddress = new Uri(options?.BaseUrl ?? "http://localhost:11434");
            client.Timeout = TimeSpan.FromSeconds(PollyPolicies.HttpTimeoutSeconds);
        });
        break;

    default:
        builder.Services.AddSingleton<ILlmCompletionClient, UnavailableLlmCompletionClient>();
        break;
}

builder.Services.AddSingleton<IEmbeddingGenerator, HashEmbeddingGenerator>();
builder.Services.AddSingleton<ISemanticCache, SemanticCacheService>();

// Configuración de SQL Server con dos entornos:
// - DefaultConnection: entorno local/testing (localdb) — usado con dotnet ef y desarrollo manual
// - ProductionConnection: entorno de producción (Azure SQL / AWS RDS)
// Solo se usa SQL Server si INTERVIEWDRILL_CONNECTION_STRING está definida como variable de entorno.
// En cualquier otro caso (tests, CI, desarrollo sin BD) se usa in-memory como fallback seguro.
var connectionString = Environment.GetEnvironmentVariable("INTERVIEWDRILL_CONNECTION_STRING");
bool useSqlServer = !string.IsNullOrWhiteSpace(connectionString);

if (useSqlServer)
{
    builder.Services.AddDbContext<InterviewDrillDbContext>(options =>
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

    builder.Services.AddScoped<IQuestionRepository, SqlQuestionRepository>();
    builder.Services.AddScoped<IQuestionRegistry, QuestionRegistry>();
    builder.Services.AddScoped<IInterviewerSessionStore>(sp =>
        new EfInterviewerSessionStore(
            sp.GetRequiredService<InterviewDrillDbContext>(),
            sp.GetRequiredService<IPiiSanitizer>()));
    builder.Services.AddScoped<IInterviewAnswerRepository, SqlInterviewAnswerRepository>();
    builder.Services.AddScoped<IStudyGuideRepository>(sp =>
        new SqlStudyGuideRepository(
            sp.GetRequiredService<InterviewDrillDbContext>(),
            sp.GetRequiredService<IPiiSanitizer>()));
    builder.Services.AddScoped<ISessionHistoryRepository, SqlSessionHistoryRepository>();
}
else
{
    builder.Services.AddSingleton<InMemoryInterviewerSessionStore>();
    builder.Services.AddSingleton<IInterviewerSessionStore>(sp => sp.GetRequiredService<InMemoryInterviewerSessionStore>());
    builder.Services.AddSingleton<InMemoryInterviewAnswerRepository>();
    builder.Services.AddSingleton<IInterviewAnswerRepository>(sp => sp.GetRequiredService<InMemoryInterviewAnswerRepository>());
    builder.Services.AddSingleton<InMemoryStudyGuideRepository>();
    builder.Services.AddSingleton<IStudyGuideRepository>(sp => sp.GetRequiredService<InMemoryStudyGuideRepository>());
    builder.Services.AddSingleton<InMemorySessionHistoryRepository>();
    builder.Services.AddSingleton<ISessionHistoryRepository>(sp => sp.GetRequiredService<InMemorySessionHistoryRepository>());
    builder.Services.AddSingleton<IQuestionRegistry, InMemoryQuestionRegistry>();
}

builder.Services.AddSingleton<IInterviewerQuestionGenerator>(services =>
{
    var llmClient = services.GetRequiredService<ILlmCompletionClient>();
    var piiSanitizer = services.GetRequiredService<IPiiSanitizer>();

    return new FallbackInterviewerQuestionGenerator(
        new LlmInterviewerQuestionGenerator(llmClient, piiSanitizer),
        new HeuristicInterviewerQuestionGenerator());
});

builder.Services.AddSingleton<IArchitectureEvaluator>(services =>
{
    var llmClient = services.GetRequiredService<ILlmCompletionClient>();
    var piiSanitizer = services.GetRequiredService<IPiiSanitizer>();
    var heuristic = new HeuristicArchitectureEvaluator(piiSanitizer);

    return new FallbackArchitectureEvaluator(
        new HybridArchitectureEvaluator(
            heuristic,
            new LlmArchitectureJudge(llmClient, piiSanitizer)),
        heuristic);
});

builder.Services.AddSingleton<ISecurityRigorJudge>(services =>
{
    var llmClient = services.GetRequiredService<ILlmCompletionClient>();
    var piiSanitizer = services.GetRequiredService<IPiiSanitizer>();
    var heuristic = new HeuristicSecurityRigorJudge(piiSanitizer);

    return new FallbackSecurityRigorJudge(
        new HybridSecurityRigorJudge(
            heuristic,
            new LlmSecurityJudge(llmClient, piiSanitizer)),
        heuristic);
});

builder.Services.AddSingleton<IScoringSynthesizer, ScoringSynthesizer>();
builder.Services.AddSingleton<VerdictDeterminer>();
builder.Services.AddScoped<ISessionHistoryService, SessionHistoryService>();
builder.Services.AddSingleton<IStudyGuideGenerator, StudyGuideGenerator>();
builder.Services.AddSingleton<IStudyGuidePdfRenderer, QuestPdfStudyGuideRenderer>();
builder.Services.AddScoped<IInterviewerAgent, InterviewerAgent>();
builder.Services.AddScoped<IInterviewAnswerProcessor, InterviewAnswerProcessor>();

builder.Services.AddScoped<IJdParserAgent>(services =>
{
    var piiSanitizer = services.GetRequiredService<IPiiSanitizer>();
    var llmClient = services.GetRequiredService<ILlmCompletionClient>();

    return new FallbackJdParserAgent(
        new LlmJdParserAgent(piiSanitizer, llmClient),
        new HeuristicJdParserAgent(piiSanitizer));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    persistence = string.IsNullOrWhiteSpace(connectionString) ? "in-memory" : "sql-server",
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("Health");

app.MapPost("/api/jd/parse", async (
    HttpContext http,
    IJdDocumentTextExtractor extractor,
    IJdParserAgent parser,
    CancellationToken cancellationToken) =>
{
    try
    {
        var jdText = await ReadJobDescriptionAsync(http, extractor, cancellationToken);

        if (string.IsNullOrWhiteSpace(jdText))
        {
            return Results.BadRequest(new ErrorResponse("Job Description text is required."));
        }

        var profile = await parser.ParseAsync(jdText, cancellationToken);
        return Results.Ok(profile);
    }
    catch (DocumentSizeExceededException ex)
    {
        return Results.Json(new ErrorResponse(ex.Message), statusCode: StatusCodes.Status413PayloadTooLarge);
    }
    catch (JdDocumentException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
    catch (JdParsingException ex)
    {
        return Results.Json(new ErrorResponse(ex.Message), statusCode: StatusCodes.Status502BadGateway);
    }
    catch (InvalidOperationException)
    {
        return Results.BadRequest(new ErrorResponse("Use multipart/form-data for file uploads or application/json/text/plain for pasted JD."));
    }
})
.WithName("ParseJobDescription")
.DisableAntiforgery();

app.MapPost("/api/interview/sessions", async (
    StartInterviewRequest request,
    IInterviewerAgent interviewer,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.UserId) || request.Profile is null)
    {
        return Results.BadRequest(new ErrorResponse("UserId and JobProfile are required."));
    }

    var sessionId = Guid.NewGuid().ToString();

    try
    {
        var question = await interviewer.StartSessionAsync(
            sessionId,
            request.UserId,
            request.Profile,
            cancellationToken,
            InterviewLanguages.Normalize(request.Language));

        return Results.Ok(new InterviewStartResponse(sessionId, question));
    }
    catch (InterviewerException ex)
    {
        return Results.Conflict(new ErrorResponse(ex.Message));
    }
})
.WithName("StartInterviewSession");

app.MapPost("/api/interview/sessions/{sessionId}/answers", async (
    string sessionId,
    SubmitAnswerRequest request,
    IInterviewAnswerProcessor processor,
    IHubContext<InterviewHub, IInterviewHub> hub,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await processor.SubmitAnswerAsync(
            new AnswerSubmission(
                sessionId,
                request.UserId,
                request.AnswerText,
                InterviewLanguages.Normalize(request.Language)),
            cancellationToken);

        await hub.Clients.Group(sessionId).FeedbackReceived(result.Feedback);

        if (result.NextQuestion is not null)
        {
            await hub.Clients.Group(sessionId).QuestionReceived(result.NextQuestion);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(result.StudyGuideMarkdown))
            {
                await hub.Clients.Group(sessionId).StudyGuideReady(result.StudyGuideMarkdown);
            }

            await hub.Clients.Group(sessionId).InterviewCompleted(sessionId);
        }

        return Results.Ok(new InterviewAnswerResponse(
            result.Feedback,
            result.NextQuestion,
            result.InterviewCompleted,
            result.StudyGuideMarkdown));
    }
    catch (UnknownInterviewSessionException ex)
    {
        return Results.NotFound(new ErrorResponse(ex.Message));
    }
    catch (AnswerRequiredException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
    catch (InterviewerException ex)
    {
        return Results.Conflict(new ErrorResponse(ex.Message));
    }
})
.WithName("SubmitInterviewAnswer");

app.MapGet("/api/interview/sessions", async (
    string userId,
    ISessionHistoryService history,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.BadRequest(new ErrorResponse("userId is required."));
    }

    var sessions = await history.GetHistoryAsync(userId, cancellationToken);
    return Results.Ok(sessions);
})
.WithName("GetInterviewHistory");

app.MapGet("/api/interview/sessions/{sessionId}/study-guide", async (
    string sessionId,
    IStudyGuideRepository guides,
    CancellationToken cancellationToken) =>
{
    var markdown = await guides.GetBySessionAsync(sessionId, cancellationToken);

    return string.IsNullOrWhiteSpace(markdown)
        ? Results.NotFound(new ErrorResponse($"Study guide for session '{sessionId}' was not found."))
        : Results.Ok(new { markdown });
})
.WithName("GetStudyGuide");

app.MapGet("/api/interview/sessions/{sessionId}/study-guide.pdf", async (
    string sessionId,
    IStudyGuideRepository guides,
    IStudyGuidePdfRenderer renderer,
    CancellationToken cancellationToken) =>
{
    var markdown = await guides.GetBySessionAsync(sessionId, cancellationToken);

    if (string.IsNullOrWhiteSpace(markdown))
    {
        return Results.NotFound(new ErrorResponse($"Study guide for session '{sessionId}' was not found."));
    }

    var pdf = renderer.Render(markdown);
    return Results.File(pdf, "application/pdf", $"study-guide-{sessionId}.pdf");
})
.WithName("GetStudyGuidePdf");

app.MapHub<InterviewHub>("/hubs/interview");

app.Run();

static async Task<string> ReadJobDescriptionAsync(
    HttpContext http,
    IJdDocumentTextExtractor extractor,
    CancellationToken cancellationToken)
{
    if (http.Request.HasFormContentType)
    {
        var form = await http.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

        if (file is not null)
        {
            await using var stream = file.OpenReadStream();
            var extracted = await extractor.ExtractAsync(stream, file.FileName, cancellationToken);
            return extracted.Text;
        }

        return form["text"].ToString();
    }

    http.Request.EnableBuffering();
    using var reader = new StreamReader(http.Request.Body, Encoding.UTF8, leaveOpen: true);
    var body = await reader.ReadToEndAsync(cancellationToken);

    if (string.IsNullOrWhiteSpace(body))
    {
        return string.Empty;
    }

    if (body.TrimStart().StartsWith('{', StringComparison.Ordinal))
    {
        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("text", out var textElement) &&
            textElement.ValueKind == JsonValueKind.String)
        {
            return textElement.GetString() ?? string.Empty;
        }
    }

    return body;
}

public partial class Program
{
}

internal sealed record ErrorResponse(string Error);
