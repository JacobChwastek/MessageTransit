namespace MessageTransit
{
    public interface IActivityObserverConnector
    {
        ConnectHandle ConnectActivityObserver(IActivityObserver observer);
    }
}
