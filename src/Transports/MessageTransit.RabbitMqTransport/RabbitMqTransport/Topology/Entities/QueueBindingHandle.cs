namespace MessageTransit.RabbitMqTransport.Topology
{
    using MessageTransit.Topology;


    public interface QueueBindingHandle :
        EntityHandle
    {
        ExchangeToQueueBinding Binding { get; }
    }
}
