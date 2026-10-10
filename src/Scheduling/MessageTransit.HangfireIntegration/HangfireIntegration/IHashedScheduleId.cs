namespace MessageTransit.HangfireIntegration
{
    public interface IHashedScheduleId
    {
        string? HashId { get; }
    }
}
