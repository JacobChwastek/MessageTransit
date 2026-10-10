namespace MessageTransit.MongoDbIntegration.Courier.Consumers
{
    using System.Threading.Tasks;
    using Events;
    using MessageTransit.Courier.Contracts;


    public class RoutingSlipActivityCompletedConsumer :
        IConsumer<RoutingSlipActivityCompleted>
    {
        readonly IRoutingSlipEventPersister _persister;

        public RoutingSlipActivityCompletedConsumer(IRoutingSlipEventPersister persister)
        {
            _persister = persister;
        }

        public Task Consume(ConsumeContext<RoutingSlipActivityCompleted> context)
        {
            var @event = new RoutingSlipActivityCompletedDocument(context.Message);

            return _persister.Persist(context.Message.TrackingNumber, @event);
        }
    }
}
