namespace MessageTransit.Contracts.JobService
{
    using System;


    public interface JobSubmissionAccepted
    {
        Guid JobId { get; }
    }
}
