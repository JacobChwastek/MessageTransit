namespace MessageTransit.AzureServiceBusTransport
{
    using Transports;


    public interface ISendEndpointContextSupervisor :
        ITransportSupervisor<SendEndpointContext>
    {
    }
}
