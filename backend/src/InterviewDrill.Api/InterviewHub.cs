using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.InterviewFlow;
using Microsoft.AspNetCore.SignalR;

namespace InterviewDrill.Api;

public interface IInterviewHub
{
    Task QuestionReceived(InterviewQuestion question);
    Task FeedbackReceived(EvaluationPayload feedback);
    Task StudyGuideReady(string markdown);
    Task InterviewCompleted(string sessionId);
}

public sealed class InterviewHub(IServiceScopeFactory scopeFactory) : Hub<IInterviewHub>
{
    public Task JoinSessionAsync(string sessionId, CancellationToken ct = default)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, sessionId, ct);
    }

    public Task LeaveSessionAsync(string sessionId, CancellationToken ct = default)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId, ct);
    }

    public async Task<InterviewStartResponse> StartSessionAsync(
        StartInterviewRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || request.Profile is null)
        {
            throw new InterviewerException("UserId and JobProfile are required.");
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var interviewer = scope.ServiceProvider.GetRequiredService<IInterviewerAgent>();

        var sessionId = Guid.NewGuid().ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId, ct);

        var question = await interviewer.StartSessionAsync(
            sessionId,
            request.UserId,
            request.Profile,
            ct,
            InterviewLanguages.Normalize(request.Language));
        await Clients.Group(sessionId).QuestionReceived(question);

        return new InterviewStartResponse(sessionId, question);
    }

    public async Task<InterviewAnswerResponse> SubmitAnswerAsync(
        string sessionId,
        SubmitAnswerRequest request,
        CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<IInterviewAnswerProcessor>();

        var result = await processor.SubmitAnswerAsync(
            new AnswerSubmission(
                sessionId,
                request.UserId,
                request.AnswerText,
                InterviewLanguages.Normalize(request.Language)),
            ct);

        await Clients.Group(sessionId).FeedbackReceived(result.Feedback);

        if (result.NextQuestion is not null)
        {
            await Clients.Group(sessionId).QuestionReceived(result.NextQuestion);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(result.StudyGuideMarkdown))
            {
                await Clients.Group(sessionId).StudyGuideReady(result.StudyGuideMarkdown);
            }

            await Clients.Group(sessionId).InterviewCompleted(sessionId);
        }

        return new InterviewAnswerResponse(
            result.Feedback,
            result.NextQuestion,
            result.InterviewCompleted,
            result.StudyGuideMarkdown);
    }
}
