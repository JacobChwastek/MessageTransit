namespace MessageTransit.Configuration
{
    public interface IHandlerConnectorCache<T>
        where T : class
    {
        IHandlerConnector<T> Connector { get; }
    }
}
