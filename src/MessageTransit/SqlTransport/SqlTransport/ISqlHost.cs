namespace MessageTransit.SqlTransport
{
    using Transports;


    public interface ISqlHost :
        IHost<ISqlReceiveEndpointConfigurator>
    {
    }
}
