#nullable enable
namespace MessageTransit
{
    public interface ISqlMessagePublishTopologyConfigurator<TMessage> :
        IMessagePublishTopologyConfigurator<TMessage>,
        ISqlMessagePublishTopology<TMessage>,
        ISqlMessagePublishTopologyConfigurator
        where TMessage : class
    {
    }


    public interface ISqlMessagePublishTopologyConfigurator :
        IMessagePublishTopologyConfigurator,
        ISqlTopicConfigurator
    {
    }
}
