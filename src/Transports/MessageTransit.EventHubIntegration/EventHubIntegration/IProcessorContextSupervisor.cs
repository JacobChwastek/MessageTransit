namespace MessageTransit.EventHubIntegration
{
    using Transports;


    public interface IProcessorContextSupervisor :
        ITransportSupervisor<ProcessorContext>
    {
    }
}
