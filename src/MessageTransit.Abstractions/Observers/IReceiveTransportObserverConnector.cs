namespace MessageTransit
{
    public interface IReceiveTransportObserverConnector
    {
        ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer);
    }
}
