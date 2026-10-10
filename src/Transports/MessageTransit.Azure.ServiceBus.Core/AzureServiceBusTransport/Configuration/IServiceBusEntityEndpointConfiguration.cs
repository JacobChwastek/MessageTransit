namespace MessageTransit.AzureServiceBusTransport.Configuration
{
    using MessageTransit.Configuration;
    using Transports;


    public interface IServiceBusEntityEndpointConfiguration :
        IReceiveEndpointConfiguration,
        IServiceBusEndpointConfiguration
    {
        void Build(IHost host);
    }
}
