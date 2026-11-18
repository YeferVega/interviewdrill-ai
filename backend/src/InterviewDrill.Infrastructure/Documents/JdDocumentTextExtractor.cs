using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using InterviewDrill.Core.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace InterviewDrill.Infrastructure.Documents;

public sealed class JdDocumentTextExtractor : IJdDocumentTextExtractor
{
    public const long MaxBytes = 2L * 1024 * 1024;

    public async Task<ExtractedJdText> ExtractAsync(
        Stream content,
        string fileName,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension is not (".pdf" or ".docx" or ".txt"))
        {
            throw new UnsupportedDocumentFormatException(extension);
        }

        using var buffered = await ReadWithLimitAsync(content, ct);
        var text = extension switch
        {
            ".pdf" => ExtractPdf(buffered),
            ".docx" => await ExtractDocxAsync(buffered),
            ".txt" => await ExtractTextAsync(buffered),
            _ => throw new UnsupportedDocumentFormatException(extension)
        };

        return new ExtractedJdText(NormalizeText(text));
    }

    private static async Task<MemoryStream> ReadWithLimitAsync(Stream content, CancellationToken ct)
    {
        var stream = new MemoryStream();
        var buffer = new byte[8192];
        long total = 0;

        int read;
        while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            total += read;
            if (total > MaxBytes)
            {
                stream.Dispose();
                throw new DocumentSizeExceededException(MaxBytes);
            }

            await stream.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        stream.Position = 0;
        return stream;
    }

    private static string ExtractPdf(Stream stream)
    {
        try
        {
            using var document = PdfDocument.Open(stream);
            var builder = new StringBuilder();

            foreach (Page page in document.GetPages())
            {
                builder.AppendLine(page.Text);
            }

            return builder.ToString();
        }
        catch (Exception ex)
        {
            throw new JdDocumentException("Failed to extract text from PDF.", ex);
        }
    }

    private static async Task<string> ExtractDocxAsync(Stream stream)
    {
        try
        {
            await using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            copy.Position = 0;

            using var document = WordprocessingDocument.Open(copy, false);
            var body = document.MainDocumentPart?.Document?.Body;
            var text = string.Join('\n', body?.Descendants<Text>().Select(t => t.Text) ?? []);
            return text;
        }
        catch (Exception ex)
        {
            throw new JdDocumentException("Failed to extract text from DOCX.", ex);
        }
    }

    private static async Task<string> ExtractTextAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private static string NormalizeText(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
    }
}
