using InterviewDrill.Core.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InterviewDrill.Infrastructure.Pdf;

public sealed class QuestPdfStudyGuideRenderer : IStudyGuidePdfRenderer
{
    static QuestPdfStudyGuideRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var blocks = ParseMarkdown(markdown);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(text => text.FontSize(11).FontColor(Colors.Grey.Darken3));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text("InterviewDrill AI").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text("Guía de estudio personalizada").FontSize(11).SemiBold();
                    });

                    row.ConstantItem(80).AlignRight().Text("Study Guide").FontSize(10).Light();
                });

                page.Content().PaddingVertical(16).Column(column =>
                {
                    column.Spacing(8);

                    foreach (var block in blocks)
                    {
                        switch (block.Kind)
                        {
                            case MarkdownBlockKind.Heading:
                                column.Item().PaddingTop(8).Text(block.Text).FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                                break;
                            case MarkdownBlockKind.Subheading:
                                column.Item().PaddingTop(14).Text(block.Text).FontSize(14).Bold().FontColor(Colors.Grey.Darken4);
                                break;
                            case MarkdownBlockKind.Topic:
                                column.Item().PaddingTop(12).Text(block.Text).FontSize(13).SemiBold().FontColor(Colors.Blue.Darken1);
                                break;
                            case MarkdownBlockKind.Bullet:
                                column.Item().PaddingLeft(12).Text($"• {StripInlineMarkdown(block.Text)}").FontSize(10.5f);
                                break;
                            case MarkdownBlockKind.HorizontalRule:
                                column.Item().PaddingVertical(8).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);
                                break;
                            default:
                                column.Item().Text(StripInlineMarkdown(block.Text)).FontSize(10.5f);
                                break;
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(9));
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static IReadOnlyList<MarkdownBlock> ParseMarkdown(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var blocks = new List<MarkdownBlock>();

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (line.Length == 0)
            {
                continue;
            }

            if (line == "---")
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.HorizontalRule, line));
            }
            else if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Topic, line[4..].Trim()));
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Subheading, line[3..].Trim()));
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading, line[2..].Trim()));
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Bullet, line[2..].Trim()));
            }
            else if (line.Length > 2 && char.IsDigit(line[0]) && line[1] == '.')
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Bullet, line));
            }
            else
            {
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Paragraph, line));
            }
        }

        return blocks;
    }

    private static string StripInlineMarkdown(string value)
    {
        return value
            .Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal)
            .Replace('`', '\'');
    }

    private enum MarkdownBlockKind
    {
        Heading,
        Subheading,
        Topic,
        Bullet,
        Paragraph,
        HorizontalRule
    }

    private sealed record MarkdownBlock(MarkdownBlockKind Kind, string Text);
}
