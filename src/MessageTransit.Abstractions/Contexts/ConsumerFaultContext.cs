namespace MessageTransit
{
    public interface ConsumerFaultContext
    {
        string MessageType { get; }
        string ConsumerType { get; }
    }
}
