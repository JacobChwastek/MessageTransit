namespace MessageTransit.KafkaIntegration.Serializers
{
    using Confluent.Kafka;


    public interface IHeadersSerializer
    {
        Headers Serialize(SendContext context);
    }
}
