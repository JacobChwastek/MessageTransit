#nullable enable
namespace MessageTransit.JobService.Messages;

using System;
using Contracts.JobService;


public class JobSubmissionAcceptedResponse :
    JobSubmissionAccepted
{
    public Guid JobId { get; set; }
}
