using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using InterviewDrill.Core.Contracts;

namespace InterviewDrill.Agents;

/// <summary>
/// Offline question registry for development/demo environments.
/// Production persistence should use SqlQuestionRepository-backed QuestionRegistry.
/// </summary>
public sealed class InMemoryQuestionRegistry : IQuestionRegistry
{
    private readonly ConcurrentDictionary<string, string> _normalizedHashes = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> WasAlreadyAskedAsync(string userId, string jdHash, string questionText, CancellationToken ct = default)
    {
        return Task.FromResult(_normalizedHashes.ContainsKey(BuildKey(userId, jdHash, questionText)));
    }

    public Task<bool> RegisterAsync(string sessionId, string userId, string jdHash, InterviewQuestion question, CancellationToken ct = default)
    {
        var key = BuildKey(userId, jdHash, question.Text);
        return Task.FromResult(_normalizedHashes.TryAdd(key, question.Id));
    }

    private static string BuildKey(string userId, string jdHash, string questionText)
    {
        var normalized = string.Join(
            ' ',
            questionText
                .Trim()
                .ToLowerInvariant()
                .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
        return $"{userId}|{jdHash}|{hash}";
    }
}
