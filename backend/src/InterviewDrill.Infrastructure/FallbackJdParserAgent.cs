using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Llm;

namespace InterviewDrill.Infrastructure;

public sealed class FallbackJdParserAgent(IJdParserAgent primary, IJdParserAgent fallback) : IJdParserAgent
{
    public async Task<JobProfile> ParseAsync(string content, CancellationToken ct = default)
    {
        try
        {
            return await primary.ParseAsync(content, ct);
        }
        catch (Exception ex) when (IsFallbackable(ex))
        {
            return await fallback.ParseAsync(content, ct);
        }
    }

    private static bool IsFallbackable(Exception ex)
    {
        return ex is LlmUnavailableException or JdParsingException;
    }
}
