using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
