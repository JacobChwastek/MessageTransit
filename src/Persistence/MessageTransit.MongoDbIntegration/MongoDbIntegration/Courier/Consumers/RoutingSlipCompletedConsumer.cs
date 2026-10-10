namespace MessageTransit.MongoDbIntegration.Courier.Consumers
{
    using System.Threading.Tasks;
    using Events;
    using MessageTransit.Courier.Contracts;


    public class RoutingSlipCompletedConsumer :
        IConsumer<RoutingSlipCompleted>
    {
        readonly IRoutingSlipEventPersister _persister;

        public RoutingSlipCompletedConsumer(IRoutingSlipEventPersister persister)
        {
            _persister = persister;
        }

        public Task Consume(ConsumeContext<RoutingSlipCompleted> context)
        {
            var @event = new RoutingSlipCompletedDocument(context.Message);

            return _persister.Persist(context.Message.TrackingNumber, @event);
        }
    }
}
