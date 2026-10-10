namespace MessageTransit.ActiveMqTransport
{
    using Transports;


    public interface IActiveMqHost :
        IHost<IActiveMqReceiveEndpointConfigurator>
    {
    }
}
