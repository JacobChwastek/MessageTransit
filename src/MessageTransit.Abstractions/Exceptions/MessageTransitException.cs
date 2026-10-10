namespace MessageTransit;

using System;
using System.Runtime.Serialization;


[Serializable]
public class MessageTransitException : Exception
{
    public MessageTransitException()
    {
    }

    public MessageTransitException(string? message)
        : base(message)
    {
    }

    public MessageTransitException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected MessageTransitException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
