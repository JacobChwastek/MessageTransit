namespace MessageTransit.KafkaIntegration.Serializers
{
    using System;
    using System.Text.Json;
    using Confluent.Kafka;
    using Serialization;


    public class MessageTransitJsonDeserializer<T> :
        IDeserializer<T>
    {
        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (data.IsEmpty && isNull)
                return default;

            return JsonSerializer.Deserialize<T>(data, SystemTextJsonMessageSerializer.Options);
        }
    }
}
