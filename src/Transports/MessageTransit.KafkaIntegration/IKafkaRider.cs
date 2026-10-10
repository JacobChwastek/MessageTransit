namespace MessageTransit
{
    using KafkaIntegration;
    using Transports;


    public interface IKafkaRider :
        IRiderControl,
        ITopicProducerProvider,
        IKafkaTopicEndpointConnector
    {
    }
}
