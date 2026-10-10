namespace MessageTransit.AzureServiceBusTransport
{
    using Transports;


    public interface IClientContextSupervisor :
        ITransportSupervisor<ClientContext>
    {
    }
}
