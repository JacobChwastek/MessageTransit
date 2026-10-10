#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class JobSlotAllocatedResponse :
    JobSlotAllocated
{
    public Guid JobId { get; set; }
    public Uri InstanceAddress { get; set; } = null!;
}
