namespace MessageTransit.AzureServiceBusTransport.Configuration
{
    public interface IServiceBusSubscriptionEndpointConfiguration :
        IServiceBusEntityEndpointConfiguration
    {
        SubscriptionSettings Settings { get; }
    }
}
