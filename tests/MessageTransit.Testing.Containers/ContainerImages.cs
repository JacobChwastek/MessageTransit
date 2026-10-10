namespace MessageTransit.Testing.Containers;

/// <summary>
/// Pinned images for the shared test containers
/// </summary>
public static class ContainerImages
{
    public const string ActiveMq = "masstransit/activemq:latest@sha256:d84a61fb86c619b067e1d79131e60c0f57e83d1cf455c7307e5e9bd681eddb0e";
    public const string Artemis = "quay.io/artemiscloud/activemq-artemis-broker:artemis.2.38.0";
    public const string Azurite = "mcr.microsoft.com/azure-storage/azurite:3.37.0";
    public const string SqlServer = "mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04";
    public const string PostgreSql = "postgres:17.10";
    public const string RabbitMq = "masstransit/rabbitmq:4.3.1";
    public const string CosmosDbEmulator = "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:2.14.28";
    public const string EventHubsEmulator = "mcr.microsoft.com/azure-messaging/eventhubs-emulator:2.2.1";
    public const string KafkaBroker = "confluentinc/cp-server:7.9.1";
    public const string KafkaSchemaRegistry = "confluentinc/cp-schema-registry:7.9.1";
    public const string KafkaZookeeper = "confluentinc/cp-zookeeper:7.9.1";
    public const string LocalStack = "localstack/localstack:3.0.2";
    public const string MongoDb = "mongo:7.0.41";
    public const string Redis = "redis:7.4.1";
    public const string Valkey = "valkey/valkey:8.1.10";
}
