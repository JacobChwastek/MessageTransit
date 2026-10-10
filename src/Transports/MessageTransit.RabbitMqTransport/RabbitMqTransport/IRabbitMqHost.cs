namespace MessageTransit.RabbitMqTransport
{
    using Transports;


    public interface IRabbitMqHost :
        IHost<IRabbitMqReceiveEndpointConfigurator>
    {
    }
}
