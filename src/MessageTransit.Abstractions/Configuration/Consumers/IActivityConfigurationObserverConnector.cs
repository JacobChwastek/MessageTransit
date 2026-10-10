namespace MessageTransit
{
    public interface IActivityConfigurationObserverConnector
    {
        ConnectHandle ConnectActivityConfigurationObserver(IActivityConfigurationObserver observer);
    }
}
