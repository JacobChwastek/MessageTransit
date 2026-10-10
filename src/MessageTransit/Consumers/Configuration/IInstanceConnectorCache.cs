namespace MessageTransit.Configuration
{
    public interface IInstanceConnectorCache<T>
        where T : class
    {
        IInstanceConnector Connector { get; }
    }
}
