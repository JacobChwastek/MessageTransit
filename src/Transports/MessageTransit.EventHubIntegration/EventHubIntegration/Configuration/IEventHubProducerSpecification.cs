namespace MessageTransit.EventHubIntegration.Configuration
{
    using Transports;


    public interface IEventHubProducerSpecification :
        ISpecification
    {
        EventHubSendTransportContext CreateSendTransportContext(string eventHubName, IBusInstance busInstance);
    }
}
