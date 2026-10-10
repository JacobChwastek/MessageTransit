namespace MessageTransit.KafkaIntegration
{
    using Transports;


    public interface IProducerContextSupervisor :
        ITransportSupervisor<ProducerContext>
    {
    }
}
