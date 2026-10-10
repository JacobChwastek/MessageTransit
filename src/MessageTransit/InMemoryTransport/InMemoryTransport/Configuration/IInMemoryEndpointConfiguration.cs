namespace MessageTransit.InMemoryTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IInMemoryEndpointConfiguration :
        IEndpointConfiguration
    {
        new IInMemoryTopologyConfiguration Topology { get; }
    }
}
