namespace MessageTransit.ActiveMqTransport.Topology
{
    using MessageTransit.Topology;


    public interface TopicHandle :
        EntityHandle
    {
        Topic Topic { get; }
    }
}
