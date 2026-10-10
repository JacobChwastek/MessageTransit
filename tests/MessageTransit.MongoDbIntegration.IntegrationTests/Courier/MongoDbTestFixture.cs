namespace MessageTransit.MongoDbIntegration.Tests.Courier;

using MongoDB.Driver;
using NUnit.Framework;
using TestFramework;


[TestFixture]
public abstract class MongoDbTestFixture : InMemoryActivityTestFixture
{
    protected const string EventCollectionName = "Events";
    protected const string EventDatabaseName = "EventStore";

    protected MongoClient Client;
    protected IMongoDatabase Database { get; set; }

    MessageTransitMongoDbConventions _convention;

    protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
    {
        base.ConfigureInMemoryBus(configurator);

        Client = new MongoClient(TestMongo.ConnectionString);

        Database = Client.GetDatabase(EventDatabaseName);

        _convention = new MessageTransitMongoDbConventions();
    }
}
