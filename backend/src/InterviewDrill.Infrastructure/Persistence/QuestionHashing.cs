using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace InterviewDrill.Infrastructure.Persistence;

public static class QuestionHashing
{
    public static string NormalizeQuestion(string question)
    {
        var spans = question
            .Trim()
            .ToLower(CultureInfo.InvariantCulture)
            .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        return string.Join(' ', spans);
    }

    public static string ComputeNormalizedHash(string question)
    {
        var normalized = NormalizeQuestion(question);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
