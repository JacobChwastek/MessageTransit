namespace MassTransit.Testing.Containers;

using Testcontainers.LocalStack;


/// <summary>
/// LocalStack (SQS, SNS, S3) bound to its default edge port 4566, which the suites address directly
/// </summary>
public static class TestLocalStack
{
    public const int EdgePort = 4566;

    public static readonly SharedContainer<LocalStackContainer> Server = new(() => new LocalStackBuilder(ContainerImages.LocalStack)
        .WithPortBinding(EdgePort, EdgePort)
        .Build());
}
