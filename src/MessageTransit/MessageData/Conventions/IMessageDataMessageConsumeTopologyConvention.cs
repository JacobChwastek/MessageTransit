namespace MessageTransit.MessageData.Conventions
{
    using MessageTransit.Configuration;


    public interface IMessageDataMessageConsumeTopologyConvention<TMessage> :
        IMessageConsumeTopologyConvention<TMessage>
        where TMessage : class
    {
    }
}
