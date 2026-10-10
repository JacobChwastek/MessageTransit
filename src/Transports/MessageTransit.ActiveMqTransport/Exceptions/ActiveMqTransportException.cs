
namespace MessageTransit;

using System;
using System.Runtime.Serialization;


[Serializable]
public class ActiveMqTransportException : MessageTransitException
{
    public ActiveMqTransportException()
    {
    }

    public ActiveMqTransportException(string message)
        : base(message)
    {
    }

    public ActiveMqTransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected ActiveMqTransportException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
