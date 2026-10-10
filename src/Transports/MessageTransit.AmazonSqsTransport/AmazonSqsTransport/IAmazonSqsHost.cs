namespace MessageTransit.AmazonSqsTransport;

using Transports;


public interface IAmazonSqsHost :
    IHost<IAmazonSqsReceiveEndpointConfigurator>
{
    new IAmazonSqsBusTopology Topology { get; }
}
