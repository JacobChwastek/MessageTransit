namespace MessageTransit.SqlTransport.Configuration
{
    using MessageTransit.Configuration;
    using Transports;


    public interface ISqlReceiveEndpointConfiguration :
        IReceiveEndpointConfiguration,
        ISqlEndpointConfiguration
    {
        ReceiveSettings Settings { get; }

        void Build(IHost host);
    }
}
