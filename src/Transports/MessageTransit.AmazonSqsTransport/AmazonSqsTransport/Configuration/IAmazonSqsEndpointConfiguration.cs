namespace MessageTransit.AmazonSqsTransport.Configuration;

using MessageTransit.Configuration;


public interface IAmazonSqsEndpointConfiguration :
    IEndpointConfiguration
{
    new IAmazonSqsTopologyConfiguration Topology { get; }
}
