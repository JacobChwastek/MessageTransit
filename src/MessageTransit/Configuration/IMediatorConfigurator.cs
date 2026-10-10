namespace MessageTransit
{
    public interface IMediatorConfigurator :
        IReceiveEndpointConfigurator,
        IConsumeObserverConnector,
        ISendObserverConnector,
        IPublishObserverConnector
    {
    }
}
