#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class RunJobCommand :
    RunJob
{
    public Guid JobId { get; set; }
}
