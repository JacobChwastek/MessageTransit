namespace MessageTransit.SqlTransport.Topology
{
    using MessageTransit.Topology;


    public interface QueueHandle :
        EntityHandle
    {
        Queue Queue { get; }
    }
}
