namespace MessageTransit.KafkaIntegration
{
    using System.Threading;
    using Confluent.Kafka;
    using MessageTransit.Middleware;


    public class KafkaClientContext :
        BasePipeContext,
        ClientContext
    {
        public KafkaClientContext(ClientConfig config, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Config = config;
        }

        public ClientConfig Config { get; }
    }
}
