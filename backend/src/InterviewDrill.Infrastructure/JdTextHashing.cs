using System.Security.Cryptography;
using System.Text;

namespace InterviewDrill.Infrastructure;

public static class JdTextHashing
{
    public static string ComputeRawTextHash(string text)
    {
        var normalized = string.Join(
            ' ',
            text.Trim()
                .ToLowerInvariant()
                .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"sha256:{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
