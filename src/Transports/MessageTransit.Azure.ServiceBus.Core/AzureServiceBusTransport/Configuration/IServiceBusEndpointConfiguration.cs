namespace MessageTransit.AzureServiceBusTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IServiceBusEndpointConfiguration :
        IEndpointConfiguration
    {
        new IServiceBusTopologyConfiguration Topology { get; }
    }
}
