namespace MessageTransit.AzureServiceBusTransport.Topology
{
    using MessageTransit.Topology;


    public interface TopicSubscriptionHandle :
        EntityHandle
    {
        TopicSubscription TopicSubscription { get; }
    }
}
