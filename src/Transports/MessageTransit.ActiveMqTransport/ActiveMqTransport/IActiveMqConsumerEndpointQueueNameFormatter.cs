namespace MessageTransit.ActiveMqTransport
{
    public interface IActiveMqConsumerEndpointQueueNameFormatter
    {
        public string Format(string topic, string endpointName);
    }
}
