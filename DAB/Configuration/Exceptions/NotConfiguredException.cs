using System.Runtime.Serialization;

namespace DAB.Configuration.Exceptions;

internal class NotConfiguredException : Exception
{
    public NotConfiguredException()
    {
    }

    public NotConfiguredException(string? message) : base(message)
    {
    }

    public NotConfiguredException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected NotConfiguredException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
