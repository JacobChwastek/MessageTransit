namespace MessageTransit.Configuration
{
    public interface ISagaConnectorCache
    {
        ISagaConnector Connector { get; }
    }
}
