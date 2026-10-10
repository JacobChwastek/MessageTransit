namespace MessageTransit.Configuration
{
    public interface ISendTransformSpecification<TMessage> :
        IPipeSpecification<SendContext<TMessage>>
        where TMessage : class
    {
    }
}
