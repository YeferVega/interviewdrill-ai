using System.Text.Json;
using System.Text.Json.Serialization;
using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;
using InterviewDrill.Core.Security;
using InterviewDrill.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace InterviewDrill.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed interviewer session store.
/// Persists the full adaptive state and materializes the User/JobDescription/Session aggregate.
/// Sanitiza PII antes de serializar y persistir el estado de sesión.
/// </summary>
public sealed class EfInterviewerSessionStore(InterviewDrillDbContext context, IPiiSanitizer piiSanitizer) : IInterviewerSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    public async Task<InterviewerSessionState?> LoadAsync(string sessionId, CancellationToken ct = default)
    {
        var session = await context.InterviewSessions
            .AsNoTracking()
            .Include(s => s.JobDescription)
            .SingleOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null || string.IsNullOrWhiteSpace(session.InterviewerStateJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<InterviewerSessionState>(session.InterviewerStateJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task SaveAsync(InterviewerSessionState state, CancellationToken ct = default)
    {
        // Sanitizar PII en el estado de sesión antes de serializar y persistir
        var sanitizedState = SanitizeSessionState(state);

        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == state.UserId, ct);

        if (user is null)
        {
            user = new UserEntity
            {
                Id = state.UserId,
                BrowserToken = state.UserId,
            };

            context.Users.Add(user);
            await context.SaveChangesAsync(ct);
        }

        var jobDescription = await context.JobDescriptions
            .SingleOrDefaultAsync(j => j.UserId == state.UserId && j.RawTextHash == state.JobDescriptionHash, ct);

        if (jobDescription is null)
        {
            jobDescription = new JobDescriptionEntity
            {
                UserId = state.UserId,
                RawTextHash = state.JobDescriptionHash,
                ParsedProfileJson = JsonSerializer.Serialize(state.Profile, JsonOptions),
            };

            context.JobDescriptions.Add(jobDescription);
            await context.SaveChangesAsync(ct);
        }
        else
        {
            jobDescription.ParsedProfileJson = JsonSerializer.Serialize(state.Profile, JsonOptions);
        }

        var session = await context.InterviewSessions
            .SingleOrDefaultAsync(s => s.Id == state.SessionId, ct);

        if (session is null)
        {
            session = new InterviewSessionEntity
            {
                Id = state.SessionId,
                UserId = state.UserId,
                JobDescriptionId = jobDescription.Id,
            };

            context.InterviewSessions.Add(session);
        }

        session.Status = InterviewSessionStatus.InProgress;

        session.InterviewerStateJson = JsonSerializer.Serialize(sanitizedState, JsonOptions);

        await context.SaveChangesAsync(ct);
    }

    public async Task MarkCompletedAsync(string sessionId, DateTimeOffset endedAt, CancellationToken ct = default)
    {
        var session = await context.InterviewSessions
            .SingleOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null)
        {
            throw new InvalidOperationException($"Cannot mark unknown interview session '{sessionId}' as completed.");
        }

        session.Status = InterviewSessionStatus.Completed;
        session.EndedAt ??= endedAt;
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Sanitiza PII en los campos sensibles del estado de sesión antes de persistir.
    /// </summary>
    private InterviewerSessionState SanitizeSessionState(InterviewerSessionState state)
    {
        // Sanitizar LastSkillTarget si contiene PII (improbable pero por seguridad)
        var sanitizedSkillTarget = state.LastSkillTarget is not null
            ? piiSanitizer.Sanitize(state.LastSkillTarget).Text
            : state.LastSkillTarget;

        return state with
        {
            LastSkillTarget = sanitizedSkillTarget
        };
    }
}
