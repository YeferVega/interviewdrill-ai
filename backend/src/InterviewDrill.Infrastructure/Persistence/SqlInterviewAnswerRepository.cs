using System.Text.Json;
using System.Text.Json.Serialization;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InterviewDrill.Infrastructure.Persistence;

public sealed class SqlInterviewAnswerRepository(InterviewDrillDbContext context) : IInterviewAnswerRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    public async Task AddAnswerAsync(
        string sessionId,
        string questionId,
        string sanitizedAnswerText,
        EvaluationPayload evaluation,
        CancellationToken ct = default)
    {
        var question = await context.Questions
            .AsNoTracking()
            .SingleOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
        {
            throw new InvalidOperationException($"Question '{questionId}' was not found for answer persistence.");
        }

        context.Answers.Add(new AnswerEntity
        {
            QuestionId = question.Id,
            Text = sanitizedAnswerText,
            EvaluationPayloadJson = JsonSerializer.Serialize(evaluation, JsonOptions),
        });
    }

    public async Task<IReadOnlyList<EvaluationPayload>> GetSessionEvaluationsAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        var payloads = await context.Answers
            .AsNoTracking()
            .Where(a => a.Question.SessionId == sessionId)
            .OrderBy(a => a.AnsweredAt)
            .Select(a => a.EvaluationPayloadJson)
            .ToListAsync(ct);

        return payloads
            .Select(ParsePayload)
            .Where(x => x is not null)
            .Cast<EvaluationPayload>()
            .ToList();
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return context.SaveChangesAsync(ct);
    }

    private static EvaluationPayload? ParsePayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EvaluationPayload>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
