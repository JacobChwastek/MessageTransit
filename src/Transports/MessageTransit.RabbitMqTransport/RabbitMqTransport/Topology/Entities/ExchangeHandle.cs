namespace MessageTransit.RabbitMqTransport.Topology
{
    using MessageTransit.Topology;


    public interface ExchangeHandle :
        EntityHandle
    {
        Exchange Exchange { get; }
    }
}
