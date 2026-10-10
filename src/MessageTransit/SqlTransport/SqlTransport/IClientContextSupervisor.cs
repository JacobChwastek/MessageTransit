namespace MessageTransit.SqlTransport
{
    using Transports;


    public interface IClientContextSupervisor :
        ITransportSupervisor<ClientContext>
    {
    }
}
