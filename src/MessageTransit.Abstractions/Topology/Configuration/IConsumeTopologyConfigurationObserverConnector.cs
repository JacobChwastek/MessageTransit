namespace MessageTransit.Configuration
{
    public interface IConsumeTopologyConfigurationObserverConnector
    {
        ConnectHandle ConnectConsumeTopologyConfigurationObserver(IConsumeTopologyConfigurationObserver observer);
    }
}
