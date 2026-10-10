namespace MessageTransit.KafkaIntegration
{
    using Transports;


    public interface IConsumerContextSupervisor :
        ITransportSupervisor<ConsumerContext>
    {
    }
}
