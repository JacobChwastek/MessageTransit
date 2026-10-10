namespace MessageTransit.ActiveMqTransport.Topology
{
    using MessageTransit.Topology;


    public interface QueueHandle :
        EntityHandle
    {
        Queue Queue { get; }
    }
}
