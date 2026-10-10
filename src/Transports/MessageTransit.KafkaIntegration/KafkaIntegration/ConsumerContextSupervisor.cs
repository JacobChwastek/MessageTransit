namespace MessageTransit.KafkaIntegration
{
    using MessageTransit.Configuration;
    using Transports;


    public class ConsumerContextSupervisor :
        TransportPipeContextSupervisor<ConsumerContext>,
        IConsumerContextSupervisor
    {
        public ConsumerContextSupervisor(IHostConfiguration hostConfiguration, IClientContextSupervisor clientContextSupervisor,
            ConsumerBuilderFactory consumerBuilderFactory)
            : base(new ConsumerContextFactory(hostConfiguration, clientContextSupervisor, consumerBuilderFactory))
        {
            clientContextSupervisor.AddConsumeAgent(this);
        }
    }
}
