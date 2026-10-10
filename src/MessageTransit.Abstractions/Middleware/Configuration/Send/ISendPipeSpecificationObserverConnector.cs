namespace MessageTransit.Configuration
{
    public interface ISendPipeSpecificationObserverConnector
    {
        ConnectHandle ConnectSendPipeSpecificationObserver(ISendPipeSpecificationObserver observer);
    }
}
