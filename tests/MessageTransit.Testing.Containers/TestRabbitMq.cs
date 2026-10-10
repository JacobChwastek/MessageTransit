namespace MessageTransit.Testing.Containers;

using DotNet.Testcontainers.Builders;
using Testcontainers.RabbitMq;


/// <summary>
/// RabbitMQ with the delayed-exchange plugin, bound to the default AMQP and management host ports the suite expects
/// </summary>
public static class TestRabbitMq
{
    public const int AmqpPort = 5672;
    public const int ManagementPort = 15672;

    public static readonly SharedContainer<RabbitMqContainer> Server = new(() => new RabbitMqBuilder(ContainerImages.RabbitMq)
        .WithUsername("guest")
        .WithPassword("guest")
        .WithPortBinding(AmqpPort, AmqpPort)
        .WithPortBinding(ManagementPort, ManagementPort)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
            .ForPort(ManagementPort)
            .ForPath("/api/overview")
            .WithBasicAuthentication("guest", "guest")))
        .Build());
}
