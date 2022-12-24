namespace SCCM.Core;

public class SccmException : Exception
{
    public SccmException(string message) : base(message)
    {
    }

    public SccmException(string message, Exception inner) : base(message, inner)
    {
    }
}