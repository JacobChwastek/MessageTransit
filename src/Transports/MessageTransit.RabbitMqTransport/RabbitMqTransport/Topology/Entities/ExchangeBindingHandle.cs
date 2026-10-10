namespace MessageTransit.RabbitMqTransport.Topology
{
    using MessageTransit.Topology;


    public interface ExchangeBindingHandle :
        EntityHandle
    {
        ExchangeToExchangeBinding Binding { get; }
    }
}
