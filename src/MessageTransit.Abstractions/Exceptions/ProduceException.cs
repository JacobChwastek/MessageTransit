namespace MessageTransit;

using System;
using System.Runtime.Serialization;


[Serializable]
public class ProduceException : MessageTransitException
{
    public ProduceException()
    {
    }

    public ProduceException(string message)
        : base(message)
    {
    }

    public ProduceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected ProduceException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
