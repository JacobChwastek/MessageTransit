namespace MessageTransit.EventHubIntegration
{
    using Transports;


    public interface IEventHubDataReceiver :
        IAgent,
        DeliveryMetrics
    {
    }
}
