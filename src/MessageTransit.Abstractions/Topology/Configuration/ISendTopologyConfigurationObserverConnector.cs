namespace MessageTransit.Configuration
{
    public interface ISendTopologyConfigurationObserverConnector
    {
        ConnectHandle ConnectSendTopologyConfigurationObserver(ISendTopologyConfigurationObserver observer);
    }
}
