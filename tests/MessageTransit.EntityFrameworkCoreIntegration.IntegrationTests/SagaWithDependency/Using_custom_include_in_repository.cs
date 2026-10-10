namespace MessageTransit.EntityFrameworkCoreIntegration.Tests.SagaWithDependency;

using System;
using System.Linq;
using System.Threading.Tasks;
using DataAccess;
using MessageTransit.Tests.Saga.Messages;
using Messages;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shared;
using Testing;


[TestFixture(typeof(SqlServerTestDbParameters), ConcurrencyMode.Optimistic)]
[TestFixture(typeof(SqlServerResiliencyTestDbParameters), ConcurrencyMode.Optimistic)]
[TestFixture(typeof(PostgresTestDbParameters), ConcurrencyMode.Optimistic)]
[TestFixture(typeof(SqlServerTestDbParameters), ConcurrencyMode.Pessimistic)]
[TestFixture(typeof(SqlServerResiliencyTestDbParameters), ConcurrencyMode.Pessimistic)]
[TestFixture(typeof(PostgresTestDbParameters), ConcurrencyMode.Pessimistic)]
public class Using_custom_include_in_repository<T> : EntityFrameworkTestFixture<T, SagaWithDependencyContext>
    where T : ITestDbParameters, new()
{
    [Test]
    public async Task A_correlated_message_should_update_inner_saga_dependency()
    {
        var sagaId = NewId.NextGuid();
        var message = new InitiateSimpleSaga(sagaId);

        await InputQueueSendEndpoint.Send(message);

        Guid? foundId = await _sagaRepository.Value.ShouldContainSaga(message.CorrelationId, TestTimeout);

        Assert.That(foundId, Is.Not.Null);

        var propertyValue = "expected saga property value";
        var updateInnerProperty = new UpdateSagaDependency(sagaId, propertyValue);

        await InputQueueSendEndpoint.Send(updateInnerProperty);

        foundId = await _sagaRepository.Value.ShouldContainSaga(x => x.CorrelationId == sagaId && x.Completed && x.Dependency.SagaInnerDependency.Name == propertyValue, TestTimeout);

        Assert.That(foundId, Is.Not.Null);
    }

    [Test]
    public async Task A_query_correlated_message_should_update_the_included_dependency_of_matching_sagas()
    {
        var sagaName = NewId.NextGuid().ToString("N");
        var first = NewId.NextGuid();
        var second = NewId.NextGuid();
        var other = NewId.NextGuid();
        // one at a time: concurrent serializable inserts can fail on PostgreSQL, and this fixture has no retry policy
        foreach (var (id, name) in new[] { (first, sagaName), (second, sagaName), (other, "other") })
        {
            await InputQueueSendEndpoint.Send(new InitiateSimpleSaga(id) { Name = name });
            Assert.That(await _sagaRepository.Value.ShouldContainSaga(id, TestTimeout), Is.Not.Null);
        }

        await InputQueueSendEndpoint.Send(new RenameSagaDependencies { SagaName = sagaName, Name = "renamed" });

        foreach (var id in new[] { first, second })
        {
            Assert.That(await _sagaRepository.Value.ShouldContainSaga(x => x.CorrelationId == id && x.Dependency.SagaInnerDependency.Name == "renamed",
                TestTimeout), Is.Not.Null);
        }

        Assert.That(await _sagaRepository.Value.ShouldContainSaga(x => x.CorrelationId == other && x.Dependency.SagaInnerDependency.Name == null,
            TestTimeout), Is.Not.Null);
    }

    [Test]
    public async Task An_initiating_message_should_start_the_saga()
    {
        var sagaId = NewId.NextGuid();
        Console.WriteLine(sagaId);
        var message = new InitiateSimpleSaga(sagaId);

        await InputQueueSendEndpoint.Send(message).ConfigureAwait(false);

        Guid? foundId = await _sagaRepository.Value.ShouldContainSaga(message.CorrelationId, TestTimeout).ConfigureAwait(false);

        Assert.That(foundId, Is.Not.Null);
    }

    readonly Lazy<ISagaRepository<SagaWithDependency>> _sagaRepository;

    public Using_custom_include_in_repository(ConcurrencyMode concurrencyMode)
    {
        Func<DbContext> contextFactory = () => new SagaWithDependencyContextFactory().CreateDbContext(DbContextOptionsBuilder);
        Func<IQueryable<SagaWithDependency>, IQueryable<SagaWithDependency>> customizeQuery =
            query => query.Include(saga => saga.Dependency).ThenInclude(dependency => dependency.SagaInnerDependency);

        _sagaRepository = new Lazy<ISagaRepository<SagaWithDependency>>(() => concurrencyMode == ConcurrencyMode.Optimistic
            ? EntityFrameworkSagaRepository<SagaWithDependency>.CreateOptimistic(contextFactory, customizeQuery)
            : EntityFrameworkSagaRepository<SagaWithDependency>.CreatePessimistic(contextFactory, RawSqlLockStatements, customizeQuery));
    }

    [OneTimeSetUp]
    public async Task SetUp()
    {
        await using var context = new SagaWithDependencyContextFactory().CreateDbContext(DbContextOptionsBuilder);

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await using var context = new SagaWithDependencyContextFactory().CreateDbContext(DbContextOptionsBuilder);

        await context.Database.EnsureDeletedAsync();
    }

    protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
    {
        configurator.Saga(_sagaRepository.Value);
    }
}
