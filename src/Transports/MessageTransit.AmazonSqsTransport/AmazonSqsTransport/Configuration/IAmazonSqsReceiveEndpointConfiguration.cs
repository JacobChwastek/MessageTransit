namespace MessageTransit.AmazonSqsTransport.Configuration;

using MessageTransit.Configuration;
using Transports;


public interface IAmazonSqsReceiveEndpointConfiguration :
    IReceiveEndpointConfiguration,
    IAmazonSqsEndpointConfiguration
{
    ReceiveSettings Settings { get; }

    void Build(IHost host);
}
