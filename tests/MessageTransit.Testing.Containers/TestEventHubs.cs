namespace MessageTransit.Testing.Containers;

using System;
using System.IO;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.Azurite;


/// <summary>
/// The Event Hubs emulator on the default AMQP host port, with the Azurite instance it uses for metadata and checkpoints
/// </summary>
public static class TestEventHubs
{
    public const int AmqpPort = 5672;

    static string _configurationFile;

    static readonly Lazy<INetwork> Network = new(() =>
    {
        var network = new NetworkBuilder().Build();
        SharedContainers.Track(network);
        network.CreateAsync().GetAwaiter().GetResult();
        return network;
    });

    static readonly SharedContainer<AzuriteContainer> Azurite = new(() => new AzuriteBuilder(ContainerImages.Azurite)
        .WithNetwork(Network.Value)
        .WithNetworkAliases("azurite")
        .Build());

    static readonly SharedContainer<IContainer> Emulator = new(() => new ContainerBuilder(ContainerImages.EventHubsEmulator)
        .WithNetwork(Network.Value)
        .WithPortBinding(AmqpPort, AmqpPort)
        .WithEnvironment("BLOB_SERVER", "azurite")
        .WithEnvironment("METADATA_SERVER", "azurite")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithResourceMapping(File.ReadAllBytes(_configurationFile), "/Eventhubs_Emulator/ConfigFiles/Config.json")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Emulator Service is Successfully Up"))
        .Build());

    /// <summary>
    /// The storage connection string for checkpoints, using the emulator's Azurite instance
    /// </summary>
    public static string StorageConnectionString => Azurite.Instance.GetConnectionString();

    /// <summary>
    /// Starts Azurite and the emulator, configured with the event hubs declared in <paramref name="configurationFile" />
    /// </summary>
    public static void Start(string configurationFile)
    {
        _configurationFile = configurationFile;

        Azurite.Start();
        Emulator.Start();
    }
}
