namespace MessageTransit.InMemoryTransport.Configuration
{
    using MessageTransit.Configuration;


    public interface IInMemoryTopologyConfiguration :
        ITopologyConfiguration
    {
        new IInMemoryPublishTopologyConfigurator Publish { get; }

        new IInMemoryConsumeTopologyConfigurator Consume { get; }
    }
}
