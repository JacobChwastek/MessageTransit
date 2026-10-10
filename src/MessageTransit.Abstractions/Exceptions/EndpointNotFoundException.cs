namespace MessageTransit;

using System;
using System.Runtime.Serialization;


[Serializable]
public class EndpointNotFoundException : MessageTransitException
{
    public EndpointNotFoundException()
    {
    }

    public EndpointNotFoundException(string message)
        : base(message)
    {
    }

    public EndpointNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected EndpointNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
