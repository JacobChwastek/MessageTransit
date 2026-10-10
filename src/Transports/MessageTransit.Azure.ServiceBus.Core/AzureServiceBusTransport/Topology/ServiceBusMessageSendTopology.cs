namespace MessageTransit.AzureServiceBusTransport.Topology
{
    using MessageTransit.Topology;


    public class ServiceBusMessageSendTopology<TMessage> :
        MessageSendTopology<TMessage>,
        IServiceBusMessageSendTopologyConfigurator<TMessage>
        where TMessage : class
    {
    }
}
