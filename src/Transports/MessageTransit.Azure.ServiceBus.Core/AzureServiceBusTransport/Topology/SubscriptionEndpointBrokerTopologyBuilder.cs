namespace MessageTransit.AzureServiceBusTransport.Topology
{
    public class SubscriptionEndpointBrokerTopologyBuilder :
        BrokerTopologyBuilder,
        ISubscriptionEndpointBrokerTopologyBuilder
    {
        public TopicHandle Topic { get; set; }
    }
}
