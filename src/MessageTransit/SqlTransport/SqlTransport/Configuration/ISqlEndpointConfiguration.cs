namespace MessageTransit.SqlTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface ISqlEndpointConfiguration :
        IEndpointConfiguration
    {
        new ISqlTopologyConfiguration Topology { get; }
    }
}
