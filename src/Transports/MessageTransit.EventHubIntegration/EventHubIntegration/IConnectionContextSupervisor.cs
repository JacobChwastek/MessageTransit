namespace MessageTransit.EventHubIntegration
{
    using Transports;


    public interface IConnectionContextSupervisor :
        ITransportSupervisor<ConnectionContext>
    {
    }
}
