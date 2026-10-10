namespace MessageTransit.Configuration
{
    public interface IConsumePipeSpecificationObserverConnector
    {
        ConnectHandle ConnectConsumePipeSpecificationObserver(IConsumePipeSpecificationObserver observer);
    }
}
