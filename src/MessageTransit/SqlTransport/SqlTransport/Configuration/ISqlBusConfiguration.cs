namespace MessageTransit.SqlTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface ISqlBusConfiguration :
        IBusConfiguration
    {
        new ISqlHostConfiguration HostConfiguration { get; }

        new ISqlEndpointConfiguration BusEndpointConfiguration { get; }

        new ISqlTopologyConfiguration Topology { get; }

        ISqlEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
    }
}
