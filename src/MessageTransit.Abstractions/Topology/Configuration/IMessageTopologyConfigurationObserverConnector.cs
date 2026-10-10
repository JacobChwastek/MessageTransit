namespace MessageTransit.Configuration
{
    public interface IMessageTopologyConfigurationObserverConnector
    {
        ConnectHandle ConnectMessageTopologyConfigurationObserver(IMessageTopologyConfigurationObserver observer);
    }
}
