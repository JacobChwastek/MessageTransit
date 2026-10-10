namespace MessageTransit.InMemoryTransport.Configuration
{
    using MessageTransit.Configuration;
    using Transports;


    public interface IInMemoryReceiveEndpointConfiguration :
        IReceiveEndpointConfiguration,
        IInMemoryEndpointConfiguration
    {
        IInMemoryReceiveEndpointConfigurator Configurator { get; }

        void Build(IHost host);
    }
}
