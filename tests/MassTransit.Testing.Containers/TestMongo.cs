namespace MassTransit.Testing.Containers;

using Testcontainers.MongoDb;


/// <summary>
/// A single-node MongoDB replica set; transactions need the replica set
/// </summary>
public static class TestMongo
{
    public static readonly SharedContainer<MongoDbContainer> Server = new(() => new MongoDbBuilder(ContainerImages.MongoDb)
        .WithReplicaSet("rs0")
        .Build());

    public static string ConnectionString => Server.Instance.GetConnectionString();
}
