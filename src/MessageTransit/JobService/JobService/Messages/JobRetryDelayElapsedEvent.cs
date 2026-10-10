#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class JobRetryDelayElapsedEvent :
    JobRetryDelayElapsed
{
    public Guid JobId { get; set; }
}
