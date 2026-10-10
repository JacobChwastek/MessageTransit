namespace MessageTransit;

using System;
using System.Runtime.Serialization;


[Serializable]
public class JobServiceStoppingException : MessageTransitException
{
    public JobServiceStoppingException()
    {
    }

    public JobServiceStoppingException(Guid jobId)
        : base($"The job service is stopping, job cannot be started: {jobId}")
    {
    }

    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
    protected JobServiceStoppingException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
