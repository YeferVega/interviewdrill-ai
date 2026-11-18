namespace InterviewDrill.Core.Contracts;

public class InterviewerException : Exception
{
    public InterviewerException(string message) : base(message)
    {
    }

    public InterviewerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class UnknownInterviewSessionException : InterviewerException
{
    public UnknownInterviewSessionException(string sessionId)
        : base($"Unknown interview session '{sessionId}'.")
    {
    }
}

public sealed class NoInterviewQuestionsLeftException : InterviewerException
{
    public NoInterviewQuestionsLeftException(string sessionId)
        : base($"No more interview questions can be generated for session '{sessionId}'.")
    {
    }
}

public sealed class AnswerRequiredException : InterviewerException
{
    public AnswerRequiredException() : base("Candidate answer text is required.")
    {
    }
}

public sealed class EvaluationParsingException : InterviewerException
{
    public EvaluationParsingException(string message) : base(message)
    {
    }

    public EvaluationParsingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class InterviewQuestionParsingException : InterviewerException
{
    public InterviewQuestionParsingException(string message) : base(message)
    {
    }

    public InterviewQuestionParsingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
