namespace MessageTransit.InMemoryTransport
{
    using Transports;


    public interface IInMemoryHost :
        IHost<IInMemoryReceiveEndpointConfigurator>
    {
        IInMemoryDelayProvider DelayProvider { get; }
    }
}
