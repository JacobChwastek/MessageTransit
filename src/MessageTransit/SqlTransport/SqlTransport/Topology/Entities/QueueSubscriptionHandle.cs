namespace MessageTransit.SqlTransport.Topology
{
    using MessageTransit.Topology;


    public interface QueueSubscriptionHandle :
        EntityHandle
    {
        TopicToQueueSubscription Subscription { get; }
    }
}
