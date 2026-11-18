namespace InterviewDrill.Core.Pdf;

public interface IStudyGuidePdfRenderer
{
    byte[] Render(string markdown);
}
