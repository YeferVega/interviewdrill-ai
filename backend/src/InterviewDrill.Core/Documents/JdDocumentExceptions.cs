namespace InterviewDrill.Core.Documents;

public class JdDocumentException : Exception
{
    public JdDocumentException(string message) : base(message)
    {
    }

    public JdDocumentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class DocumentSizeExceededException : JdDocumentException
{
    public DocumentSizeExceededException(long maximumBytes)
        : base($"Job Description exceeds maximum size of {maximumBytes} bytes.")
    {
    }
}

public sealed class UnsupportedDocumentFormatException : JdDocumentException
{
    public UnsupportedDocumentFormatException(string extension)
        : base($"Unsupported Job Description format '{extension}'. Supported formats are PDF, DOCX and TXT.")
    {
    }
}
