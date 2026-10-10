namespace MessageTransit.MessageData.Conventions
{
    using MessageTransit.Configuration;


    public interface IMessageDataMessageSendTopologyConvention<TMessage> :
        IMessageSendTopologyConvention<TMessage>
        where TMessage : class
    {
    }
}
