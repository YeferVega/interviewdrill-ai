using System.Text;
using InterviewDrill.Infrastructure.Pdf;

namespace InterviewDrill.Infrastructure.Tests.Pdf;

public sealed class QuestPdfStudyGuideRendererTests
{
    [Fact]
    public void Render_ReturnsValidPdfBytes()
    {
        var renderer = new QuestPdfStudyGuideRenderer();
        const string markdown = """
            # Guía de estudio para AI Engineer (Senior)

            ## Temas prioritarios
            ### Resiliencia
            - Practica retry con backoff.
            - Explica circuit breaker.

            ---
            Página de prueba con acentos: seguridad, métricas P95/P99.
            """;

        var pdf = renderer.Render(markdown);

        Assert.NotNull(pdf);
        Assert.NotEmpty(pdf);

        var header = Encoding.ASCII.GetString(pdf[..5]);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void Render_HandlesEmptyGuide()
    {
        var renderer = new QuestPdfStudyGuideRenderer();

        var pdf = renderer.Render(string.Empty);

        Assert.NotEmpty(pdf);
    }
}
