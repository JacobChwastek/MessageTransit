namespace MessageTransit.ActiveMqTransport.Configuration
{
    using MessageTransit.Configuration;
    using Transports;


    public interface IActiveMqReceiveEndpointConfiguration :
        IReceiveEndpointConfiguration,
        IActiveMqEndpointConfiguration
    {
        ReceiveSettings Settings { get; }

        void Build(IHost host);
    }
}
