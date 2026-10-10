namespace MessageTransit;

public interface IAmazonSqsMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
