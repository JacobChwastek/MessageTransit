namespace MessageTransit
{
    using EventHubIntegration;
    using Transports;


    public interface IEventHubReceiveEndpointContext :
        ReceiveEndpointContext
    {
        IProcessorContextSupervisor ContextSupervisor { get; }
    }
}
