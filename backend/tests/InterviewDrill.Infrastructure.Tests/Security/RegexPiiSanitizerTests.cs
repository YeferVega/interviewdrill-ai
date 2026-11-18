using System.Text.RegularExpressions;
using InterviewDrill.Core.Security;
using InterviewDrill.Infrastructure.Security;

namespace InterviewDrill.Infrastructure.Tests.Security;

public class RegexPiiSanitizerTests
{
    [Theory]
    [InlineData("Contact: alice@example.com", "Contact: [EMAIL]")]
    [InlineData("Call +1 555-123-4567", "Call [PHONE]")]
    [InlineData("SSN 123-45-6789", "SSN [SSN]")]
    [InlineData("Card 4111 1111 1111 1111", "Card [CREDIT_CARD]")]
    [InlineData("Server 192.168.1.10", "Server [IP]")]
    [InlineData("IPv6 fe80:0000:0000:0000:0204:61ff:254:2f3f", "IPv6 [IP]")]
    public void Sanitize_MasksKnownPii(string input, string expected)
    {
        var sanitizer = new RegexPiiSanitizer();

        var result = sanitizer.Sanitize(input);

        Assert.Equal(expected, result.Text);
    }

    [Fact]
    public void Sanitize_DetectOnlyMode_DoesNotChangeTextButReturnsFindings()
    {
        var sanitizer = new RegexPiiSanitizer();
        const string input = "Email alice@example.com";

        var result = sanitizer.Sanitize(input, mask: false);

        Assert.Equal(input, result.Text);
        var finding = Assert.Single(result.Findings);
        Assert.Equal("Email", finding.Type);
        Assert.Equal("[EMAIL]", finding.Placeholder);
        Assert.Equal("alice@example.com".Length, finding.Length);
    }

    [Fact]
    public void Sanitize_SelectsHigherPriorityPatternForOverlaps()
    {
        var sanitizer = new RegexPiiSanitizer();
        const string input = "Card 4111-1111-1111-1111";

        var result = sanitizer.Sanitize(input);

        Assert.Equal("Card [CREDIT_CARD]", result.Text);
        var finding = Assert.Single(result.Findings);
        Assert.Equal("CreditCard", finding.Type);
    }

    [Fact]
    public void Sanitize_MasksContextualName()
    {
        var sanitizer = new RegexPiiSanitizer();
        const string input = "Name: Juan Pérez";

        var result = sanitizer.Sanitize(input);

        Assert.Equal("Name: [NAME]", result.Text);
        Assert.Contains(result.Findings, f => f.Type == "Name");
    }

    [Fact]
    public void AddPattern_SupportsCustomSensitivePattern()
    {
        var sanitizer = new RegexPiiSanitizer();
        sanitizer.AddPattern(new PiiPatternDefinition(
            "ApiKey",
            "[API_KEY]",
            @"sk-[A-Za-z0-9]{20,}",
            RegexOptions.Compiled,
            Priority: 110));

        const string input = "Use sk-abcdefghijklmnopqrstuvwxyz123456 now";

        var result = sanitizer.Sanitize(input);

        Assert.Equal("Use [API_KEY] now", result.Text);
    }
}
