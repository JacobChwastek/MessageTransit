namespace MassTransit;

using System;
using System.Runtime.Serialization;


[Serializable]
public class CourierException : MassTransitException
{
    public CourierException()
    {
    }

    public CourierException(string message)
        : base(message)
    {
    }

    public CourierException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected CourierException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
