namespace MessageTransit.Contracts.JobService
{
    public enum JobStatus
    {
        Running,
        Faulted,
        Completed,
        Canceled
    }
}
