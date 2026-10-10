#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class FinalizeJobCommand :
    FinalizeJob
{
    public Guid JobId { get; set; }
}
