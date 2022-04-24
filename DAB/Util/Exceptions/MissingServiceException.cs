using System.Runtime.Serialization;

namespace DAB.Util.Exceptions;

internal class MissingServiceException : Exception
{
    public MissingServiceException()
    {
    }

    public MissingServiceException(string? message) : base(message)
    {
    }

    public MissingServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected MissingServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
