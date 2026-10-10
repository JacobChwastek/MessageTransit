namespace MessageTransit.AmazonSqsTransport.Topology;

using MessageTransit.Topology;


public interface QueueHandle :
    EntityHandle
{
    Queue Queue { get; }
}
