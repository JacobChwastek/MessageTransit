namespace MessageTransit
{
    public interface IDeadLetterQueueNameFormatter
    {
        string FormatDeadLetterQueueName(string queueName);
    }
}
