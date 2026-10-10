namespace MessageTransit.RabbitMqTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IRabbitMqTopologyConfiguration :
        ITopologyConfiguration
    {
        new IRabbitMqPublishTopologyConfigurator Publish { get; }

        new IRabbitMqSendTopologyConfigurator Send { get; }

        new IRabbitMqConsumeTopologyConfigurator Consume { get; }
    }
}
