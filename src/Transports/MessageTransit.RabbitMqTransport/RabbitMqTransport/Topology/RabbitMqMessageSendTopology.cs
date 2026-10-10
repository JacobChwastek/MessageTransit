namespace MessageTransit.RabbitMqTransport.Topology
{
    using MessageTransit.Topology;


    public class RabbitMqMessageSendTopology<TMessage> :
        MessageSendTopology<TMessage>,
        IRabbitMqMessageSendTopologyConfigurator<TMessage>
        where TMessage : class
    {
    }
}
