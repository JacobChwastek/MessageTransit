namespace MessageTransit.AmazonSqsTransport.Topology;

using MessageTransit.Topology;


public interface QueueSubscriptionHandle :
    EntityHandle
{
    QueueSubscription QueueSubscription { get; }
}
