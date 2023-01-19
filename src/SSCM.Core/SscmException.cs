namespace SSCM.Core;

public class SscmException : Exception
{
    public SscmException()
    {

    }
    public SscmException(string message) : base(message)
    {
    }

    public SscmException(string message, Exception inner) : base(message, inner)
    {
    }
}