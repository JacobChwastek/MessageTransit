namespace MessageTransit
{
    using Azure;


    public interface IServiceBusNamedKeyTokenProviderConfigurator
    {
        AzureNamedKeyCredential NamedKeyCredential { set; }
    }
}
