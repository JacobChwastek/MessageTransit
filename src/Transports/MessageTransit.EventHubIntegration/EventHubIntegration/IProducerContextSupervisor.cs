namespace MessageTransit.EventHubIntegration
{
    using Transports;


    public interface IProducerContextSupervisor :
        ITransportSupervisor<ProducerContext>
    {
    }
}
