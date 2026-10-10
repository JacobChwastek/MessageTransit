namespace MessageTransit.ActiveMqTransport.Topology
{
    using MessageTransit.Topology;


    public class ActiveMqMessageSendTopology<TMessage> :
        MessageSendTopology<TMessage>,
        IActiveMqMessageSendTopologyConfigurator<TMessage>
        where TMessage : class
    {
    }
}
