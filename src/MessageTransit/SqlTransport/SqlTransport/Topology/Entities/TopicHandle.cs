namespace MessageTransit.SqlTransport.Topology
{
    using MessageTransit.Topology;


    public interface TopicHandle :
        EntityHandle
    {
        Topic Topic { get; }
    }
}
