namespace MessageTransit;

using System;
using System.Runtime.Serialization;


[Serializable]
public class PayloadException : MessageTransitException
{
    public PayloadException()
    {
    }

    public PayloadException(string message)
        : base(message)
    {
    }

    public PayloadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected PayloadException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
