using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using InterviewDrill.Core.Documents;
using InterviewDrill.Infrastructure.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfDocument = QuestPDF.Fluent.Document;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;
using Xunit;

namespace InterviewDrill.Infrastructure.Tests.Documents;

public sealed class JdDocumentTextExtractorTests
{
    static JdDocumentTextExtractorTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task ExtractAsync_SupportsTxt()
    {
        var extractor = new JdDocumentTextExtractor();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Senior Backend Engineer\nRequired: RAG"));

        var result = await extractor.ExtractAsync(stream, "job.txt");

        Assert.Contains("Senior Backend Engineer", result.Text);
        Assert.Contains("RAG", result.Text);
    }

    [Fact]
    public async Task ExtractAsync_SupportsDocx()
    {
        var extractor = new JdDocumentTextExtractor();
        await using var docx = CreateDocx("Lead AI Engineer Kafka");

        var result = await extractor.ExtractAsync(docx, "job.docx");

        Assert.Contains("Lead AI Engineer Kafka", result.Text);
    }

    [Fact]
    public async Task ExtractAsync_SupportsPdf_HappyPath()
    {
        // Generate a real, text-bearing PDF with QuestPDF (a dependency we already ship)
        // and feed it to the extractor so the PdfPig path is exercised end-to-end.
        var pdf = CreatePdf("Senior Backend Engineer RAG Kafka OpenTelemetry");

        var extractor = new JdDocumentTextExtractor();
        await using var stream = new MemoryStream(pdf);

        var result = await extractor.ExtractAsync(stream, "job.pdf");

        Assert.Contains("Senior Backend Engineer", result.Text);
        Assert.Contains("RAG", result.Text);
        Assert.Contains("Kafka", result.Text);
    }

    [Fact]
    public async Task ExtractAsync_RejectsUnsupportedExtension()
    {
        var extractor = new JdDocumentTextExtractor();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        await Assert.ThrowsAsync<UnsupportedDocumentFormatException>(() =>
            extractor.ExtractAsync(stream, "job.exe"));
    }

    [Fact]
    public async Task ExtractAsync_RejectsFilesOverTwoMb()
    {
        var extractor = new JdDocumentTextExtractor();
        await using var stream = new MemoryStream(new byte[JdDocumentTextExtractor.MaxBytes + 1]);

        await Assert.ThrowsAsync<DocumentSizeExceededException>(() =>
            extractor.ExtractAsync(stream, "job.txt"));
    }

    [Fact]
    public async Task ExtractAsync_NullContent_Throws()
    {
        var extractor = new JdDocumentTextExtractor();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            extractor.ExtractAsync(null!, "job.txt"));
    }

    [Fact]
    public async Task ExtractAsync_NormalizesExtensionCase()
    {
        var extractor = new JdDocumentTextExtractor();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Senior Backend"));

        var result = await extractor.ExtractAsync(stream, "job.TXT");

        Assert.Equal("Senior Backend", result.Text);
    }

    [Fact]
    public async Task ExtractAsync_InvalidPdf_ThrowsJdDocumentException()
    {
        var extractor = new JdDocumentTextExtractor();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("no es un pdf"));

        await Assert.ThrowsAsync<JdDocumentException>(() =>
            extractor.ExtractAsync(stream, "job.pdf"));
    }

    [Fact]
    public async Task ExtractAsync_InvalidDocx_ThrowsJdDocumentException()
    {
        var extractor = new JdDocumentTextExtractor();
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("no es un docx zip valido"));

        await Assert.ThrowsAsync<JdDocumentException>(() =>
            extractor.ExtractAsync(stream, "job.docx"));
    }

    [Fact]
    public async Task ExtractAsync_EmptyTxt_ReturnsEmptyText()
    {
        var extractor = new JdDocumentTextExtractor();
        await using var stream = new MemoryStream();

        var result = await extractor.ExtractAsync(stream, "empty.txt");

        Assert.Equal(string.Empty, result.Text);
    }

    private static MemoryStream CreateDocx(string text)
    {
        var stream = new MemoryStream();

        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new WordDocument(
                new Body(
                    new Paragraph(
                        new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }))));
            main.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }

    private static byte[] CreatePdf(string text)
    {
        return PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.Content().Text(text);
            });
        }).GeneratePdf();
    }
}
