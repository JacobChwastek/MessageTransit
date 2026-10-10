namespace MessageTransit.AzureServiceBusTransport.Configuration
{
    public interface IServiceBusReceiveEndpointConfiguration :
        IServiceBusEntityEndpointConfiguration
    {
        ReceiveSettings Settings { get; }
    }
}
