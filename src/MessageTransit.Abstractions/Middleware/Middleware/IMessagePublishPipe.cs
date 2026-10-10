namespace MessageTransit.Middleware
{
    public interface IMessagePublishPipe<in TMessage> :
        IPipe<PublishContext<TMessage>>
        where TMessage : class
    {
    }
}
