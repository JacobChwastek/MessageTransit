namespace MessageTransit.MongoDbIntegration.Courier.Consumers
{
    using System.Threading.Tasks;
    using Events;
    using MessageTransit.Courier.Contracts;


    public class RoutingSlipActivityFaultedConsumer :
        IConsumer<RoutingSlipActivityFaulted>
    {
        readonly IRoutingSlipEventPersister _persister;

        public RoutingSlipActivityFaultedConsumer(IRoutingSlipEventPersister persister)
        {
            _persister = persister;
        }

        public Task Consume(ConsumeContext<RoutingSlipActivityFaulted> context)
        {
            var @event = new RoutingSlipActivityFaultedDocument(context.Message);

            return _persister.Persist(context.Message.TrackingNumber, @event);
        }
    }
}
