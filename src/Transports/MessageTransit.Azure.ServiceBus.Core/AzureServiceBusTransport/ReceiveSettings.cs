namespace MessageTransit.AzureServiceBusTransport
{
    using Azure.Messaging.ServiceBus.Administration;


    public interface ReceiveSettings :
        ClientSettings
    {
        CreateQueueOptions GetCreateQueueOptions();
    }
}
