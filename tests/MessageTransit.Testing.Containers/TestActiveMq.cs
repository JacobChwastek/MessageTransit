namespace MessageTransit.Testing.Containers;

using System;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;


/// <summary>
/// Classic ActiveMQ on its default host ports, and Artemis on 61618 for the Artemis test cases
/// </summary>
public static class TestActiveMq
{
    public const string Username = "admin";
    public const string Password = "admin";
    public const int ArtemisPort = 61618;

    public static readonly SharedContainer<IContainer> Classic = new(() => new ContainerBuilder(ContainerImages.ActiveMq)
        .WithEnvironment("ACTIVEMQ_ADMIN_LOGIN", Username)
        .WithEnvironment("ACTIVEMQ_ADMIN_PASSWORD", Password)
        .WithEnvironment("ACTIVEMQ_OPTS", "-Xms512m -Xmx512m")
        .WithEnvironment("ACTIVEMQ_CONFIG_SCHEDULERENABLED", "true")
        .WithPortBinding(61616, 61616)
        .WithPortBinding(5672, 5672)
        .WithPortBinding(8161, 8161)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(61616).UntilInternalTcpPortIsAvailable(5672))
        .Build());

    public static readonly SharedContainer<IContainer> Artemis = new(() => new ContainerBuilder(ContainerImages.Artemis)
        .WithEnvironment("AMQ_USER", Username)
        .WithEnvironment("AMQ_PASSWORD", Password)
        .WithPortBinding(ArtemisPort, 61616)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(61616))
        .Build());

    /// <summary>
    /// Starts Artemis and returns its AMQP address
    /// </summary>
    public static Uri ArtemisAmqpAddress
    {
        get
        {
            Artemis.Start();
            return new Uri($"amqp://localhost:{ArtemisPort}");
        }
    }
}
