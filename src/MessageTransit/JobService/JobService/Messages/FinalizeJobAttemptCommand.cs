#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class FinalizeJobAttemptCommand :
    FinalizeJobAttempt
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
}
