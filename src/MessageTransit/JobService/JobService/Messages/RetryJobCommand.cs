#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class RetryJobCommand :
    RetryJob
{
    public Guid JobId { get; set; }
}
