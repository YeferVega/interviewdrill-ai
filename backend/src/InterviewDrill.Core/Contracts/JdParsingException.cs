namespace InterviewDrill.Core.Contracts;

public class JdParsingException : Exception
{
    public JdParsingException(string message) : base(message)
    {
    }

    public JdParsingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
