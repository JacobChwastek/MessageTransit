namespace MessageTransit.ActiveMqTransport.Topology
{
    using MessageTransit.Topology;


    public interface ConsumerHandle :
        EntityHandle
    {
        Consumer Consumer { get; }
    }
}
