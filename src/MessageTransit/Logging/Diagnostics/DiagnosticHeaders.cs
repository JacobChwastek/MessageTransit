#nullable enable
namespace MessageTransit.Logging
{
    public static class DiagnosticHeaders
    {
        public const string DefaultListenerName = "MessageTransit";

        public const string DiagnosticId = "Diagnostic-Id";
        public const string ActivityId = "MT-Activity-Id";
        public const string ActivityCorrelationContext = "MT-Activity-Correlation-Context";
        public const string ActivityPropagation = "MT-Activity-Propagation";

        public const string MessageId = "messaging.messagetransit.message_id";
        public const string CorrelationId = "messaging.messagetransit.correlation_id";
        public const string InitiatorId = "messaging.messagetransit.initiator_id";
        public const string RequestId = "messaging.messagetransit.request_id";
        public const string SourceAddress = "messaging.messagetransit.source_address";
        public const string DestinationAddress = "messaging.messagetransit.destination_address";
        public const string InputAddress = "messaging.messagetransit.input_address";
        public const string TrackingNumber = "messaging.messagetransit.tracking_number";

        public const string MessageTypes = "messaging.messagetransit.message_types";

        public const string ConsumerType = "messaging.messagetransit.consumer_type";

        public const string PeerAddress = "peer.address";

        public const string BeginState = "messaging.messagetransit.begin_state";
        public const string EndState = "messaging.messagetransit.end_state";
        public const string SagaId = "messaging.messagetransit.saga_id";


        public class Exceptions
        {
            public const string EventName = "exception";
            public const string Type = "exception.type";
            public const string Message = "exception.message";
            public const string Escaped = "exception.escaped";
            public const string Stacktrace = "exception.stacktrace";
        }


        public static class Messaging
        {
            public const string BodyLength = "messaging.message.body.size";
            public const string ConversationId = "messaging.message.conversation_id";
            public const string DestinationName = "messaging.destination.name";
            public const string TransportMessageId = "messaging.message.id";
            public const string Operation = "messaging.operation";
            public const string System = "messaging.system";


            public static class RabbitMq
            {
                public const string RoutingKey = "messaging.rabbitmq.destination.routing_key";
            }
        }
    }
}
