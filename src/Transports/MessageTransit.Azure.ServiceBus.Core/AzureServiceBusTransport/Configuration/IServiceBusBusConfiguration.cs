namespace MessageTransit.AzureServiceBusTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IServiceBusBusConfiguration :
        IBusConfiguration,
        IServiceBusEndpointConfiguration
    {
        new IServiceBusHostConfiguration HostConfiguration { get; }

        new IServiceBusEndpointConfiguration BusEndpointConfiguration { get; }

        new IServiceBusTopologyConfiguration Topology { get; }

        IServiceBusEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
    }
}
