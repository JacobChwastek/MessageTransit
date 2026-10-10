namespace MessageTransit.Testing.Containers;

using Testcontainers.Azurite;


/// <summary>
/// The Azurite storage emulator (blob, queue, and table services)
/// </summary>
public static class TestAzurite
{
    public static readonly SharedContainer<AzuriteContainer> Server = new(() => new AzuriteBuilder(ContainerImages.Azurite).Build());

    public static string ConnectionString => Server.Instance.GetConnectionString();
}
