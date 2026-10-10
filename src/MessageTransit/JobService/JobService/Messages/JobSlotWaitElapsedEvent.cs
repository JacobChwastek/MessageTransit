#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class JobSlotWaitElapsedEvent :
    JobSlotWaitElapsed
{
    public Guid JobId { get; set; }
}
