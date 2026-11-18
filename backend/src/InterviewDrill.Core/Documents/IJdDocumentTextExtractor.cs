namespace InterviewDrill.Core.Documents;

public sealed record ExtractedJdText(string Text);

public interface IJdDocumentTextExtractor
{
    Task<ExtractedJdText> ExtractAsync(
        Stream content,
        string fileName,
        CancellationToken ct = default);
}
