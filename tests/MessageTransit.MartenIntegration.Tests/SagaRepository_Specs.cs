namespace MessageTransit.MartenIntegration.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JasperFx;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using MessageTransit.Saga;
using TestFramework;
using Testing;


[TestFixture]
public class Using_the_Marten_saga_repository
{
    [Test]
    public async Task Should_find_sagas_by_a_dictionary_value()
    {
        var group = NewId.NextGuid().ToString("N");
        List<Guid> ids = await StoreSagas(group, 2);

        IEnumerable<Guid> found = await _repository.Find(new SagaQuery<AttributeSaga>(x => x.Attributes["group"] == group));

        Assert.That(found, Is.EquivalentTo(ids));
    }

    [Test]
    public async Task Should_not_inject_sql_through_a_dictionary_key()
    {
        await StoreSagas(NewId.NextGuid().ToString("N"), 2);

        var key = "missing' = '' or 1=1 --";

        IEnumerable<Guid> found = await _repository.Find(new SagaQuery<AttributeSaga>(x => x.Attributes[key] == "value"));

        Assert.That(found, Is.Empty);
    }

    [Test]
    public async Task Should_save_changes_made_by_a_consumed_message()
    {
        var id = (await StoreSagas("tracked", 1)).Single();

        await _harness.Bus.Publish(new ChangeAttribute(id, "changed"));

        Assert.That(await WaitForState(id, "changed"), Is.EqualTo("changed"));
    }

    [Test]
    public async Task Should_not_save_changes_when_the_saga_faults()
    {
        var id = (await StoreSagas("rollback", 1)).Single();

        await _harness.Bus.Publish(new ChangeAttributeAndFail(id));

        Assert.That(await _harness.Published.Any<Fault<ChangeAttributeAndFail>>(x => x.Context.Message.Message.CorrelationId == id), Is.True);

        await using var session = _store.QuerySession();
        var saga = await session.LoadAsync<AttributeSaga>(id);

        Assert.That(saga.Attributes, Does.Not.ContainKey("state"));
    }

    [Test]
    public async Task Should_reject_an_update_based_on_a_stale_version()
    {
        var id = (await StoreSagas("concurrency", 1)).Single();

        await using var first = _store.DirtyTrackedSession();
        await using var second = _store.DirtyTrackedSession();

        var firstSaga = await first.LoadAsync<AttributeSaga>(id);
        var secondSaga = await second.LoadAsync<AttributeSaga>(id);

        firstSaga.Attributes["state"] = "first";
        first.Store(firstSaga);
        await first.SaveChangesAsync();

        secondSaga.Attributes["state"] = "second";
        second.Store(secondSaga);

        Assert.That(async () => await second.SaveChangesAsync(), Throws.InstanceOf<ConcurrencyException>());
    }

    const string ConnectionString = "server=localhost;port=5432;database=MartenTest;user id=postgres;password=Password12!;";
    const string MaintenanceConnectionString = "server=localhost;port=5432;database=postgres;user id=postgres;password=Password12!;";

    ServiceProvider _provider;
    ITestHarness _harness;
    IDocumentStore _store;
    IQuerySagaRepository<AttributeSaga> _repository;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var services = new ServiceCollection();

        services.AddMarten(options =>
            {
                options.Connection(ConnectionString);
                options.CreateDatabasesForTenants(c =>
                {
                    c.MaintenanceDatabase(MaintenanceConnectionString);
                    c.ForTenant()
                        .CheckAgainstPgDatabase()
                        .WithOwner("postgres")
                        .WithEncoding("UTF-8")
                        .ConnectionLimit(-1);
                });
            })
            .ApplyAllDatabaseChangesOnStartup();

        services.AddMessageTransitTestHarness(x =>
        {
            x.AddSaga<AttributeSaga>().MartenRepository();
        });

        _provider = services.BuildServiceProvider(true);
        _harness = _provider.GetTestHarness();

        await _harness.Start();

        _store = _provider.GetRequiredService<IDocumentStore>();
        _repository = _provider.GetRequiredService<IQuerySagaRepository<AttributeSaga>>();
    }

    [OneTimeTearDown]
    public async Task Teardown()
    {
        await _provider.DisposeAsync();
    }

    async Task<List<Guid>> StoreSagas(string group, int count)
    {
        List<Guid> ids = Enumerable.Range(0, count).Select(_ => NewId.NextGuid()).ToList();

        await using var session = _store.LightweightSession();
        foreach (var id in ids)
            session.Store(new AttributeSaga { CorrelationId = id, Attributes = { ["group"] = group } });

        await session.SaveChangesAsync();

        return ids;
    }

    async Task<string> WaitForState(Guid id, string expected)
    {
        var timer = Stopwatch.StartNew();
        string state;
        do
        {
            await using var session = _store.QuerySession();
            var saga = await session.LoadAsync<AttributeSaga>(id);
            state = saga.Attributes.GetValueOrDefault("state");
            if (state == expected)
                break;

            await Task.Delay(100);
        }
        while (timer.Elapsed < _harness.TestTimeout);

        return state;
    }
}


public class AttributeSaga :
    ISaga,
    Orchestrates<ChangeAttribute>,
    Orchestrates<ChangeAttributeAndFail>
{
    public Dictionary<string, string> Attributes { get; set; } = new();

    public Guid CorrelationId { get; set; }

    public Task Consume(ConsumeContext<ChangeAttribute> context)
    {
        Attributes["state"] = context.Message.Value;

        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<ChangeAttributeAndFail> context)
    {
        Attributes["state"] = "changed";

        throw new IntentionalTestException("The saga failed after changing its state");
    }
}


public record ChangeAttribute(Guid CorrelationId, string Value) :
    CorrelatedBy<Guid>;


public record ChangeAttributeAndFail(Guid CorrelationId) :
    CorrelatedBy<Guid>;
