namespace MessageTransit
{
    public interface IServiceBusMessageSendTopology<TMessage> :
        IMessageSendTopology<TMessage>
        where TMessage : class
    {
    }
}
