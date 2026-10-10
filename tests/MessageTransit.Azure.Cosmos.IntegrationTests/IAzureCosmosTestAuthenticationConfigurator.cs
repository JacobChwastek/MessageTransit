namespace MessageTransit.Azure.Cosmos.Tests
{
    public interface IAzureCosmosTestAuthenticationConfigurator
    {
        void Configure(ICosmosSagaRepositoryConfigurator configurator);
    }
}
