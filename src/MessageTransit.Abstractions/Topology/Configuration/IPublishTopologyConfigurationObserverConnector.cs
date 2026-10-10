namespace MessageTransit.Configuration
{
    public interface IPublishTopologyConfigurationObserverConnector
    {
        ConnectHandle ConnectPublishTopologyConfigurationObserver(IPublishTopologyConfigurationObserver observer);
    }
}
