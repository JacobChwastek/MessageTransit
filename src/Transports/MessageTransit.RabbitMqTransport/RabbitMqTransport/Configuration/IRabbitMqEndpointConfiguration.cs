namespace MessageTransit.RabbitMqTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IRabbitMqEndpointConfiguration :
        IEndpointConfiguration
    {
        new IRabbitMqTopologyConfiguration Topology { get; }
    }
}
