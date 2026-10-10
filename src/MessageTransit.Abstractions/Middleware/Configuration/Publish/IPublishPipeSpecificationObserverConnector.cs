namespace MessageTransit.Configuration
{
    public interface IPublishPipeSpecificationObserverConnector
    {
        ConnectHandle ConnectPublishPipeSpecificationObserver(IPublishPipeSpecificationObserver observer);
    }
}
