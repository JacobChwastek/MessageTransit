namespace MessageTransit.SqlTransport.Topology
{
    using MessageTransit.Topology;


    public interface TopicSubscriptionHandle :
        EntityHandle
    {
        TopicToTopicSubscription Subscription { get; }
    }
}
