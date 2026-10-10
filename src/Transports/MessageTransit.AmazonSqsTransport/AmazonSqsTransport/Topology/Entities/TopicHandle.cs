namespace MessageTransit.AmazonSqsTransport.Topology;

using MessageTransit.Topology;


public interface TopicHandle :
    EntityHandle
{
    Topic Topic { get; }
}
