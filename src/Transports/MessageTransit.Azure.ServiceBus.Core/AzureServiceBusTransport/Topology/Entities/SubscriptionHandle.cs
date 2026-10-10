namespace MessageTransit.AzureServiceBusTransport.Topology
{
    using MessageTransit.Topology;


    public interface SubscriptionHandle :
        EntityHandle
    {
        Subscription Subscription { get; }
    }
}
