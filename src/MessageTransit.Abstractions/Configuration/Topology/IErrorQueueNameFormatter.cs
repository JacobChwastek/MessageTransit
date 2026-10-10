namespace MessageTransit
{
    public interface IErrorQueueNameFormatter
    {
        string FormatErrorQueueName(string queueName);
    }
}
