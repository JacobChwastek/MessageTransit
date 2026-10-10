namespace MassTransit.Testing.Containers;

using System.Net.Http;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;


/// <summary>
/// The Cosmos DB emulator on its default host ports; it advertises https://localhost:8081, so the ports cannot be remapped
/// </summary>
public static class TestCosmos
{
    public const int GatewayPort = 8081;
    static readonly int[] DirectPorts = [10250, 10251, 10252, 10253, 10254, 10255, 10256];

    public static readonly SharedContainer<IContainer> Emulator = new(() =>
    {
        var builder = new ContainerBuilder(ContainerImages.CosmosDbEmulator)
            .WithPortBinding(GatewayPort, GatewayPort)
            .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "3")
            .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "false")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                .UsingTls()
                .UsingHttpMessageHandler(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true })
                .ForPort(GatewayPort)
                .ForPath("/_explorer/emulator.pem")));

        foreach (var port in DirectPorts)
            builder = builder.WithPortBinding(port, port);

        return builder.Build();
    });
}
