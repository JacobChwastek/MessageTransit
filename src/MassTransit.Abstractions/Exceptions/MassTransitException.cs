namespace MassTransit;

using System;
using System.Runtime.Serialization;


[Serializable]
public class MassTransitException : Exception
{
    public MassTransitException()
    {
    }

    public MassTransitException(string? message)
        : base(message)
    {
    }

    public MassTransitException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected MassTransitException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
