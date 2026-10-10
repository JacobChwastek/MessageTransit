namespace MessageTransit.KafkaIntegration
{
    using System.Threading;
    using Confluent.Kafka;
    using MessageTransit.Middleware;


    public class SharedClientContext :
        ProxyPipeContext,
        ClientContext
    {
        readonly ClientContext _context;

        public SharedClientContext(ClientContext context, CancellationToken cancellationToken)
            : base(context)
        {
            _context = context;
            CancellationToken = cancellationToken;
        }

        public override CancellationToken CancellationToken { get; }

        public ClientConfig Config => _context.Config;
    }
}
