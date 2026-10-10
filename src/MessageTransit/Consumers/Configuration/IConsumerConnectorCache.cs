namespace MessageTransit.Configuration
{
    public interface IConsumerConnectorCache
    {
        IConsumerConnector Connector { get; }
    }
}
