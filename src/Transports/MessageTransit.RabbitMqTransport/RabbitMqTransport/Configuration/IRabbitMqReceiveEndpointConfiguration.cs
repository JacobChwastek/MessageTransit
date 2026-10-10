namespace MessageTransit.RabbitMqTransport.Configuration
{
    using MessageTransit.Configuration;
    using Transports;


    public interface IRabbitMqReceiveEndpointConfiguration :
        IReceiveEndpointConfiguration,
        IRabbitMqEndpointConfiguration
    {
        ReceiveSettings Settings { get; }

        void Build(IHost host);
    }
}
