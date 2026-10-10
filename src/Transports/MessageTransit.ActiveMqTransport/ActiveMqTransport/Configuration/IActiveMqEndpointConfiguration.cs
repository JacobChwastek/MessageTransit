namespace MessageTransit.ActiveMqTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IActiveMqEndpointConfiguration :
        IEndpointConfiguration
    {
        new IActiveMqTopologyConfiguration Topology { get; }
    }
}
