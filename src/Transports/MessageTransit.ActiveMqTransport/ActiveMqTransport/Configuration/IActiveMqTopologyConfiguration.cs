namespace MessageTransit.ActiveMqTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IActiveMqTopologyConfiguration :
        ITopologyConfiguration
    {
        new IActiveMqPublishTopologyConfigurator Publish { get; }

        new IActiveMqSendTopologyConfigurator Send { get; }

        new IActiveMqConsumeTopologyConfigurator Consume { get; }
    }
}
