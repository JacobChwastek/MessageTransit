namespace MessageTransit.KafkaIntegration
{
    using Confluent.Kafka;


    public interface ClientContext :
        PipeContext
    {
        ClientConfig Config { get; }
    }
}
