namespace MessageTransit.AmazonSqsTransport.Configuration;

using MessageTransit.Configuration;


public interface IAmazonSqsTopologyConfiguration :
    ITopologyConfiguration
{
    new IAmazonSqsPublishTopologyConfigurator Publish { get; }

    new IAmazonSqsSendTopologyConfigurator Send { get; }

    new IAmazonSqsConsumeTopologyConfigurator Consume { get; }
}
