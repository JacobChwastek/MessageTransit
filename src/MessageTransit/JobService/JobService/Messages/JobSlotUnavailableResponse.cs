#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class JobSlotUnavailableResponse :
    JobSlotUnavailable
{
    public Guid JobId { get; set; }
}
