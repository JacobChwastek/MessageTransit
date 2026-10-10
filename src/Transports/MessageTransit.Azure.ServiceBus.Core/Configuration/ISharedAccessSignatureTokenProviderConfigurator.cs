namespace MessageTransit
{
    using Azure;


    public interface ISharedAccessSignatureTokenProviderConfigurator
    {
        AzureSasCredential SasCredential { set; }
    }
}
