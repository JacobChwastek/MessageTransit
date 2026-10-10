namespace MessageTransit.KafkaIntegration.Serializers
{
    using System.Net.Mime;
    using Confluent.Kafka;
    using Serialization;


    public class DefaultKafkaSerializerFactory :
        IKafkaSerializerFactory
    {
        public ContentType ContentType => SystemTextJsonMessageSerializer.JsonContentType;

        public IDeserializer<T> GetDeserializer<T>()
        {
            return DeserializerTypes.TryGet<T>() ?? new MessageTransitJsonDeserializer<T>();
        }

        public IAsyncSerializer<T> GetSerializer<T>()
        {
            return SerializerTypes.TryGet<T>() ?? new MessageTransitAsyncJsonSerializer<T>();
        }
    }
}
