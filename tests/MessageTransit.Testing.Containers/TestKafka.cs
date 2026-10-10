namespace MessageTransit.Testing.Containers;

using System;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;


/// <summary>
/// ZooKeeper, a Confluent broker advertised on localhost:9092, and Schema Registry on localhost:8081
/// </summary>
public static class TestKafka
{
    public const int BrokerPort = 9092;
    public const int SchemaRegistryPort = 8081;

    static readonly Lazy<INetwork> Network = new(() =>
    {
        var network = new NetworkBuilder().Build();
        SharedContainers.Track(network);
        network.CreateAsync().GetAwaiter().GetResult();
        return network;
    });

    static readonly SharedContainer<IContainer> Zookeeper = new(() => new ContainerBuilder(ContainerImages.KafkaZookeeper)
        .WithNetwork(Network.Value)
        .WithNetworkAliases("zookeeper")
        .WithEnvironment("ZOOKEEPER_CLIENT_PORT", "2181")
        .WithEnvironment("ZOOKEEPER_TICK_TIME", "2000")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(2181))
        .Build());

    static readonly SharedContainer<IContainer> Broker = new(() => new ContainerBuilder(ContainerImages.KafkaBroker)
        .WithNetwork(Network.Value)
        .WithNetworkAliases("broker")
        .WithPortBinding(BrokerPort, BrokerPort)
        .WithEnvironment("KAFKA_BROKER_ID", "1")
        .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181")
        .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
        .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", $"PLAINTEXT://broker:29092,PLAINTEXT_HOST://localhost:{BrokerPort}")
        .WithEnvironment("KAFKA_METRIC_REPORTERS", "io.confluent.metrics.reporter.ConfluentMetricsReporter")
        .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
        .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
        .WithEnvironment("KAFKA_CONFLUENT_LICENSE_TOPIC_REPLICATION_FACTOR", "1")
        .WithEnvironment("KAFKA_CONFLUENT_BALANCER_TOPIC_REPLICATION_FACTOR", "1")
        .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
        .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
        .WithEnvironment("KAFKA_CONFLUENT_SCHEMA_REGISTRY_URL", "http://schema-registry:8081")
        .WithEnvironment("CONFLUENT_METRICS_REPORTER_BOOTSTRAP_SERVERS", "broker:29092")
        .WithEnvironment("CONFLUENT_METRICS_REPORTER_TOPIC_REPLICAS", "1")
        .WithEnvironment("CONFLUENT_METRICS_ENABLE", "true")
        .WithEnvironment("CONFLUENT_SUPPORT_CUSTOMER_ID", "anonymous")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("started \\(kafka.server.KafkaServer\\)"))
        .Build());

    static readonly SharedContainer<IContainer> SchemaRegistry = new(() => new ContainerBuilder(ContainerImages.KafkaSchemaRegistry)
        .WithNetwork(Network.Value)
        .WithNetworkAliases("schema-registry")
        .WithPortBinding(SchemaRegistryPort, SchemaRegistryPort)
        .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
        .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "broker:29092")
        .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", $"http://0.0.0.0:{SchemaRegistryPort}")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(SchemaRegistryPort).ForPath("/subjects")))
        .Build());

    /// <summary>
    /// Starts ZooKeeper, the broker, and Schema Registry in dependency order
    /// </summary>
    public static void Start()
    {
        Zookeeper.Start();
        Broker.Start();
        SchemaRegistry.Start();
    }
}
