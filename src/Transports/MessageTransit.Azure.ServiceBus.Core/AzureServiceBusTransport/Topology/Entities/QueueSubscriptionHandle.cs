namespace MessageTransit.AzureServiceBusTransport.Topology
{
    using MessageTransit.Topology;


    public interface QueueSubscriptionHandle :
        EntityHandle
    {
        QueueSubscription QueueSubscription { get; }
    }
}
