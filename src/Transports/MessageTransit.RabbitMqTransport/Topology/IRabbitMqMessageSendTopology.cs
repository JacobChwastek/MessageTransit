namespace MessageTransit
{
    public interface IRabbitMqMessageSendTopology<TMessage> :
        IMessageSendTopology<TMessage>
        where TMessage : class
    {
    }
}
