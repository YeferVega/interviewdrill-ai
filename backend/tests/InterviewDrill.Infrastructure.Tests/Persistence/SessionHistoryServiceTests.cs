using InterviewDrill.Core.Contracts;
using InterviewDrill.Core.Persistence;
using InterviewDrill.Infrastructure.Persistence;

namespace InterviewDrill.Infrastructure.Tests.Persistence;

public sealed class SessionHistoryServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_ReturnsEmptyListForUnknownUser()
    {
        var repository = new FakeSessionHistoryRepository();
        var service = new SessionHistoryService(repository);

        var history = await service.GetHistoryAsync("unknown");

        Assert.Empty(history);
    }

    [Theory]
    [InlineData(new double[] { 90, 95 }, 92.5, Verdict.Pass)]
    [InlineData(new double[] { 70, 60 }, 65, Verdict.NeedsPractice)]
    [InlineData(new double[] { 50, 30 }, 40, Verdict.Fail)]
    [InlineData(new double[] { }, 0, Verdict.Fail)]
    public async Task GetHistoryAsync_CalculatesAverageVerdict(
        double[] scores,
        double expectedScore,
        Verdict expectedVerdict)
    {
        var now = new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);
        var repository = new FakeSessionHistoryRepository
        {
            Sessions =
            [
                new SessionHistorySourceRecord(
                    "session-1",
                    now,
                    now.AddMinutes(20),
                    InterviewSessionRecordStatus.Completed,
                    scores,
                    "guide-1",
                    now.AddMinutes(21))
            ]
        };

        var service = new SessionHistoryService(repository);
        var history = await service.GetHistoryAsync("user-1");

        var summary = Assert.Single(history);
        Assert.Equal(expectedScore, summary.OverallScore);
        Assert.Equal(expectedVerdict, summary.OverallVerdict);
        Assert.Equal("guide-1", summary.StudyGuideId);
    }

    [Fact]
    public async Task GetHistoryAsync_OrdersByStartedAtDescending()
    {
        var baseTime = new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);
        var repository = new FakeSessionHistoryRepository
        {
            Sessions =
            [
                new SessionHistorySourceRecord("old", baseTime.AddDays(-2), null, InterviewSessionRecordStatus.Completed, [80], null, null),
                new SessionHistorySourceRecord("middle", baseTime.AddDays(-1), null, InterviewSessionRecordStatus.Completed, [85], null, null),
                new SessionHistorySourceRecord("new", baseTime, null, InterviewSessionRecordStatus.Completed, [90], null, null)
            ]
        };

        var service = new SessionHistoryService(repository);
        var history = await service.GetHistoryAsync("user-1");

        Assert.Equal(["new", "middle", "old"], history.Select(h => h.SessionId));
    }

    private sealed class FakeSessionHistoryRepository : ISessionHistoryRepository
    {
        public IReadOnlyList<SessionHistorySourceRecord> Sessions { get; init; } = [];

        public Task<IReadOnlyList<SessionHistorySourceRecord>> GetByUserAsync(
            string userId,
            CancellationToken ct = default)
        {
            return Task.FromResult(Sessions);
        }
    }
}
