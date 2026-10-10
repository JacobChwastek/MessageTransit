namespace MessageTransit;

using AmazonSqsTransport.Topology;


public interface IAmazonSqsConsumeTopologySpecification :
    ISpecification
{
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
