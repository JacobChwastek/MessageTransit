namespace MassTransit.EntityFrameworkCoreIntegration.Tests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql;
using NUnit.Framework;
using Shared;

[TestFixture(typeof(SqlServerTestDbParameters), ConcurrencyMode.Optimistic)]
[TestFixture(typeof(SqlServerResiliencyTestDbParameters), ConcurrencyMode.Optimistic)]
[TestFixture(typeof(PostgresTestDbParameters), ConcurrencyMode.Optimistic)]
[TestFixture(typeof(SqlServerTestDbParameters), ConcurrencyMode.Pessimistic)]
[TestFixture(typeof(SqlServerResiliencyTestDbParameters), ConcurrencyMode.Pessimistic)]
[TestFixture(typeof(PostgresTestDbParameters), ConcurrencyMode.Pessimistic)]
[Category("Integration")]
[NonParallelizable]
public class SagaBehaviorParity_Specs<TParameters> : EntityFrameworkTestFixture<TParameters, ParitySagaDbContext>
    where TParameters : ITestDbParameters, new()
{
    readonly ConcurrencyMode _concurrencyMode;
    readonly string _connectionString;
    readonly ConcurrentDictionary<Guid, bool> _insertSavedBeforeBegin = new();
    readonly DbContextOptions<ParitySagaDbContext> _options;
    readonly ParitySaveObserver _saves = new();
    TaskCompletionSource<bool> _concurrentLoads;
    TaskCompletionSource<bool> _lockHeld;
    TaskCompletionSource<bool> _probeEntered;
    TaskCompletionSource<bool> _releaseLock;
    int _incrementAttempts;
    int _groupAttempts;

    public SagaBehaviorParity_Specs(ConcurrencyMode concurrencyMode)
    {
        _concurrencyMode = concurrencyMode;
        _options = new DbContextOptionsBuilder<ParitySagaDbContext>(DbContextOptionsBuilder.Options).AddInterceptors(_saves).Options;
        using var context = new ParitySagaDbContext(DbContextOptionsBuilder.Options);
        var databaseName = $"SagaParity_{Guid.NewGuid():N}";
        var connectionString = context.Database.GetConnectionString();
        _connectionString = context.Database.IsNpgsql()
            ? new NpgsqlConnectionStringBuilder(connectionString) { Database = databaseName }.ConnectionString
            : new SqlConnectionStringBuilder(connectionString) { InitialCatalog = databaseName }.ConnectionString;
    }

    [OneTimeSetUp]
    public async Task Create_database()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestCancellationToken);
    }

    [OneTimeTearDown]
    public async Task Delete_database()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task Should_persist_factory_fields_and_initial_behavior()
    {
        var id = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = id, Name = "original", Group = "initial" });

        var saga = await Load(id);
        Assert.Multiple(() =>
        {
            Assert.That(saga, Is.Not.Null);
            Assert.That(saga.Name, Is.EqualTo("original"));
            Assert.That(saga.Group, Is.EqualTo("initial"));
            Assert.That(saga.CurrentState, Is.EqualTo("Running"));
            Assert.That(saga.Count, Is.EqualTo(1));
            Assert.That(saga.Version, Is.EqualTo(1));
            Assert.That(_insertSavedBeforeBegin.GetValueOrDefault(id), Is.True, "InsertOnInitial must save the instance before the initial behavior");
        });
    }

    [Test]
    public async Task Should_load_existing_saga_after_duplicate_initial_insert()
    {
        var id = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = id, Name = "original", Group = "duplicate" });
        await Send(new ParityBegin { CorrelationId = id, Name = "replacement", Group = "replacement" });

        var saga = await Load(id);
        Assert.Multiple(() =>
        {
            Assert.That(saga.Name, Is.EqualTo("original"));
            Assert.That(saga.Group, Is.EqualTo("duplicate"));
            Assert.That(saga.Count, Is.EqualTo(2));
            Assert.That(saga.Version, Is.EqualTo(2));
            Assert.That(_saves.FailedInserts.ContainsKey(id), Is.True, "The duplicate initial message must attempt its own insert before loading");
        });
        await using var context = CreateContext();
        Assert.That(await context.Set<ParitySaga>().CountAsync(x => x.CorrelationId == id, TestCancellationToken), Is.EqualTo(1));
    }

    [Test]
    public async Task Should_transition_a_previously_persisted_initial_instance()
    {
        var id = NewId.NextGuid();
        await Send(new ParityWarmUp { CorrelationId = id, Name = "warm", Group = "existing" });
        Assert.That((await Load(id)).CurrentState, Is.EqualTo("Initial"));

        await Send(new ParityStart { CorrelationId = id });
        var saga = await Load(id);
        Assert.Multiple(() =>
        {
            Assert.That(saga.CurrentState, Is.EqualTo("Running"));
            Assert.That(saga.Name, Is.EqualTo("warm"));
            Assert.That(saga.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Should_discard_a_missing_instance_without_persisting_it()
    {
        var id = NewId.NextGuid();
        await Send(new ParityFinish { CorrelationId = id });
        Assert.That(await Load(id), Is.Null);
    }

    [Test]
    public async Task Should_remove_a_finalized_instance()
    {
        var id = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = id, Name = "finish", Group = "finish" });
        Assert.That(await Load(id), Is.Not.Null);

        await Send(new ParityFinish { CorrelationId = id });
        Assert.That(await Load(id), Is.Null);
    }

    [Test]
    public async Task Should_not_persist_an_instance_completed_during_initial_behavior()
    {
        var id = NewId.NextGuid();
        await Send(new ParityCompleteImmediately { CorrelationId = id });
        Assert.That(await Load(id), Is.Null);
    }

    [Test]
    public async Task Should_roll_back_preinsert_when_initial_behavior_faults()
    {
        var id = NewId.NextGuid();
        Task<ConsumeContext<Fault<ParityBegin>>> fault = await ConnectPublishHandler<Fault<ParityBegin>>(x => x.Message.Message.CorrelationId == id);

        await Send(new ParityBegin { CorrelationId = id, Name = "fault", Group = "fault", Fail = true }, expectFault: true);
        var received = await fault.WaitAsync(TestTimeout, TestCancellationToken);
        Assert.That(received.Message.Exceptions.Any(x => HasMessage(x, "Initial parity fault")), Is.True);
        Assert.That(_insertSavedBeforeBegin.GetValueOrDefault(id), Is.True, "InsertOnInitial must save the instance before the initial behavior");
        Assert.That(await Load(id), Is.Null);
    }

    [Test]
    public async Task Should_persist_every_concurrent_increment_with_configured_retry()
    {
        var id = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = id, Name = "concurrent", Group = "concurrent" });
        _incrementAttempts = 0;
        _concurrentLoads = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        const int incrementCount = 8;

        await Task.WhenAll(Enumerable.Range(0, incrementCount)
            .Select(_ => Send(new ParityIncrement { CorrelationId = id, Synchronize = true }))).WaitAsync(TestTimeout, TestCancellationToken);

        var saga = await Load(id);
        Assert.Multiple(() =>
        {
            Assert.That(saga.Count, Is.EqualTo(1 + incrementCount));
            Assert.That(saga.Version, Is.EqualTo(1 + incrementCount));
            if (_concurrencyMode == ConcurrencyMode.Optimistic)
                Assert.That(_incrementAttempts, Is.GreaterThan(incrementCount), "Two initial loads share a version, so at least one must be retried");
        });
    }

    [Test]
    public async Task Should_keep_a_second_consumer_out_of_a_locked_instance()
    {
        Assume.That(_concurrencyMode, Is.EqualTo(ConcurrencyMode.Pessimistic), "Only the pessimistic repository locks the loaded row");

        var id = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = id, Name = "locked", Group = "locked" });
        _lockHeld = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _probeEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _releaseLock = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var holder = Send(new ParityIncrement { CorrelationId = id, HoldLock = true });
        await _lockHeld.Task.WaitAsync(TestTimeout, TestCancellationToken);

        var probe = Send(new ParityIncrement { CorrelationId = id, Probe = true });
        var first = await Task.WhenAny(_probeEntered.Task, Task.Delay(TimeSpan.FromSeconds(2), TestCancellationToken));
        Assert.That(first, Is.Not.SameAs(_probeEntered.Task), "The second consumer entered the saga while the first held the row lock");

        _releaseLock.TrySetResult(true);
        await Task.WhenAll(holder, probe).WaitAsync(TestTimeout, TestCancellationToken);

        var saga = await Load(id);
        Assert.Multiple(() =>
        {
            Assert.That(_probeEntered.Task.IsCompleted, Is.True);
            Assert.That(saga.Count, Is.EqualTo(3));
            Assert.That(saga.Version, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Should_not_persist_changes_from_a_readonly_event()
    {
        var id = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = id, Name = "read-only", Group = "read-only" });

        await Send(new ParityReadOnly { CorrelationId = id });
        var saga = await Load(id);
        Assert.Multiple(() =>
        {
            Assert.That(saga.Count, Is.EqualTo(1));
            Assert.That(saga.Version, Is.EqualTo(1));
            Assert.That(saga.CurrentState, Is.EqualTo("Running"));
        });
    }

    [Test]
    public async Task Should_update_every_instance_matching_a_query()
    {
        var group = NewId.NextGuid().ToString();
        var first = NewId.NextGuid();
        var second = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = first, Name = "first", Group = group });
        await Send(new ParityBegin { CorrelationId = second, Name = "second", Group = group });
        var control = await BeginControl();

        await Send(new ParityGroupIncrement { Group = group });
        var firstSaga = await Load(first);
        var secondSaga = await Load(second);
        Assert.Multiple(() =>
        {
            Assert.That(firstSaga.Count, Is.EqualTo(2));
            Assert.That(secondSaga.Count, Is.EqualTo(2));
        });
        await AssertUntouched(control);
    }

    [Test]
    public async Task Should_discard_a_query_without_matches()
    {
        var group = NewId.NextGuid().ToString();
        var control = await BeginControl();
        await Send(new ParityGroupIncrement { Group = group });

        await using var context = CreateContext();
        Assert.That(await context.Set<ParitySaga>().AnyAsync(x => x.Group == group, TestCancellationToken), Is.False);
        await AssertUntouched(control);
    }

    [Test]
    public async Task Should_roll_back_an_earlier_saved_query_instance_when_a_later_instance_faults()
    {
        var group = NewId.NextGuid().ToString();
        var first = NewId.NextGuid();
        var second = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = first, Name = "first", Group = group });
        await Send(new ParityBegin { CorrelationId = second, Name = "second", Group = group });
        var control = await BeginControl();
        _groupAttempts = 0;
        Task<ConsumeContext<Fault<ParityGroupIncrement>>> fault =
            await ConnectPublishHandler<Fault<ParityGroupIncrement>>(x => x.Message.Message.Group == group);

        await Send(new ParityGroupIncrement { Group = group, FailOnSecond = true }, expectFault: true);
        var received = await fault.WaitAsync(TestTimeout, TestCancellationToken);
        Assert.That(received.Message.Exceptions.Any(x => HasMessage(x, "Query parity fault")), Is.True);

        var firstSaga = await Load(first);
        var secondSaga = await Load(second);
        Assert.Multiple(() =>
        {
            Assert.That(_groupAttempts, Is.EqualTo(2));
            Assert.That(firstSaga.Count, Is.EqualTo(1));
            Assert.That(secondSaga.Count, Is.EqualTo(1));
            Assert.That(firstSaga.Version, Is.EqualTo(1));
            Assert.That(secondSaga.Version, Is.EqualTo(1));
        });
        await AssertUntouched(control);
    }

    [Test]
    public async Task Should_reject_a_stale_version_without_overwriting_the_committed_value()
    {
        var id = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = id, Name = "version", Group = "version" });
        await using var firstContext = CreateContext();
        await using var secondContext = CreateContext();
        var first = await firstContext.Set<ParitySaga>().SingleAsync(x => x.CorrelationId == id, TestCancellationToken);
        var second = await secondContext.Set<ParitySaga>().SingleAsync(x => x.CorrelationId == id, TestCancellationToken);

        first.Count = 5;
        first.Version++;
        await firstContext.SaveChangesAsync(TestCancellationToken);
        second.Count = 9;
        second.Version++;

        Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await secondContext.SaveChangesAsync(TestCancellationToken));
        var persisted = await Load(id);
        Assert.Multiple(() =>
        {
            Assert.That(persisted.Count, Is.EqualTo(5));
            Assert.That(persisted.Version, Is.EqualTo(2));
        });
    }

    protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
    {
        configurator.ConcurrentMessageLimit = 16;
        configurator.UseMessageRetry(r =>
        {
            r.Handle<DbUpdateConcurrencyException>();
            r.Handle<PostgresException>(exception => exception.SqlState == PostgresErrorCodes.SerializationFailure
                || exception.SqlState == PostgresErrorCodes.DeadlockDetected);
            r.Immediate(20);
        });
        configurator.UseInMemoryOutbox();
        var machine = new ParityStateMachine(BeforeBegin, BeforeIncrement, BeforeGroupIncrement);
        var repository = _concurrencyMode == ConcurrencyMode.Optimistic
            ? EntityFrameworkSagaRepository<ParitySaga>.CreateOptimistic(CreateContext)
            : EntityFrameworkSagaRepository<ParitySaga>.CreatePessimistic(CreateContext, RawSqlLockStatements);
        configurator.StateMachineSaga(machine, repository);
    }

    void BeforeBegin(BehaviorContext<ParitySaga, ParityBegin> context)
    {
        _insertSavedBeforeBegin.TryAdd(context.Saga.CorrelationId, _saves.SavedInserts.ContainsKey(context.Saga.CorrelationId));
    }

    async Task BeforeIncrement(BehaviorContext<ParitySaga, ParityIncrement> context)
    {
        var attempt = Interlocked.Increment(ref _incrementAttempts);
        if (context.Message.HoldLock)
        {
            _lockHeld.TrySetResult(true);
            await _releaseLock.Task.WaitAsync(TestTimeout, TestCancellationToken);
        }

        if (context.Message.Probe)
            _probeEntered.TrySetResult(true);

        if (_concurrencyMode == ConcurrencyMode.Optimistic && context.Message.Synchronize && attempt <= 2)
        {
            if (attempt == 2)
                _concurrentLoads.TrySetResult(true);
            await _concurrentLoads.Task.WaitAsync(TestTimeout, TestCancellationToken);
        }
    }

    void BeforeGroupIncrement(BehaviorContext<ParitySaga, ParityGroupIncrement> context)
    {
        if (context.Message.FailOnSecond && Interlocked.Increment(ref _groupAttempts) == 2)
            throw new InvalidOperationException("Query parity fault");
    }

    ParitySagaDbContext CreateContext()
    {
        var context = new ParitySagaDbContext(_options);
        context.Database.SetConnectionString(_connectionString);
        return context;
    }

    async Task<Guid> BeginControl()
    {
        var id = NewId.NextGuid();
        await Send(new ParityBegin { CorrelationId = id, Name = "control", Group = NewId.NextGuid().ToString() });
        return id;
    }

    async Task AssertUntouched(Guid id)
    {
        var saga = await Load(id);
        Assert.Multiple(() =>
        {
            Assert.That(saga.CurrentState, Is.EqualTo("Running"), "An instance outside the query must not change");
            Assert.That(saga.Count, Is.EqualTo(1), "An instance outside the query must not change");
            Assert.That(saga.Version, Is.EqualTo(1), "An instance outside the query must not change");
        });
    }

    async Task<ParitySaga> Load(Guid id)
    {
        await using var context = CreateContext();
        return await context.Set<ParitySaga>().AsNoTracking().SingleOrDefaultAsync(x => x.CorrelationId == id, TestCancellationToken);
    }

    static bool HasMessage(ExceptionInfo exception, string message)
    {
        return exception.Message == message || exception.InnerException != null && HasMessage(exception.InnerException, message);
    }

    async Task Send<TMessage>(TMessage message, bool expectFault = false)
        where TMessage : class
    {
        var messageId = NewId.NextGuid();
        var observer = new ParityReceiveObserver(messageId);
        using var handle = Bus.ConnectReceiveObserver(observer);
        await InputQueueSendEndpoint.Send(message, x => x.MessageId = messageId, TestCancellationToken);
        var faulted = await observer.Completed.WaitAsync(TestTimeout, TestCancellationToken);
        Assert.That(faulted, Is.EqualTo(expectFault), $"Unexpected receive outcome for {typeof(TMessage).Name}: {observer.Exception}");
        if (!expectFault)
            Assert.That(observer.IsDelivered, Is.True, $"The saga must consume {typeof(TMessage).Name}, including discarded messages");
    }
}

public class ParitySagaDbContext : SagaDbContext
{
    public ParitySagaDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new ParitySagaMap(); }
    }
}

public class ParitySaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public int Count { get; set; }
    public int Version { get; set; }
}

class ParitySagaMap : SagaClassMap<ParitySaga>
{
    protected override void Configure(EntityTypeBuilder<ParitySaga> entity, ModelBuilder model)
    {
        entity.ToTable("SagaBehaviorParity");
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.Name).HasMaxLength(128);
        entity.Property(x => x.Group).HasMaxLength(128);
        entity.Property(x => x.Count);
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}

class ParityStateMachine : MassTransitStateMachine<ParitySaga>
{
    public ParityStateMachine(Action<BehaviorContext<ParitySaga, ParityBegin>> beforeBegin,
        Func<BehaviorContext<ParitySaga, ParityIncrement>, Task> beforeIncrement,
        Action<BehaviorContext<ParitySaga, ParityGroupIncrement>> beforeGroupIncrement)
    {
        InstanceState(x => x.CurrentState);
        Event(() => Began, x =>
        {
            x.InsertOnInitial = true;
            x.SetSagaFactory(context => new ParitySaga
            {
                CorrelationId = context.CorrelationId.Value,
                Name = context.Message.Name,
                Group = context.Message.Group
            });
        });
        Event(() => WarmedUp, x =>
        {
            x.InsertOnInitial = true;
            x.SetSagaFactory(context => new ParitySaga
            {
                CorrelationId = context.CorrelationId.Value,
                Name = context.Message.Name,
                Group = context.Message.Group
            });
        });
        Event(() => Finished, x => x.OnMissingInstance(m => m.Discard()));
        Event(() => ReadOnlyChecked, x => x.ReadOnly = true);
        Event(() => GroupIncremented, x =>
        {
            x.CorrelateBy(saga => saga.Group, context => context.Message.Group);
            x.OnMissingInstance(m => m.Discard());
        });

        Initially(
            When(WarmedUp).Then(context => { }),
            When(Started)
                .Then(context =>
                {
                    context.Saga.Count++;
                    context.Saga.Version++;
                })
                .TransitionTo(Running),
            When(CompletedImmediately).Finalize());

        During(Initial, Running,
            When(Began)
                .Then(beforeBegin)
                .Then(context =>
                {
                    if (context.Message.Fail)
                        throw new InvalidOperationException("Initial parity fault");
                    context.Saga.Count++;
                    context.Saga.Version++;
                })
                .TransitionTo(Running));

        During(Running,
            When(Incremented)
                .ThenAsync(beforeIncrement)
                .Then(context =>
                {
                    context.Saga.Count++;
                    context.Saga.Version++;
                }),
            When(GroupIncremented)
                .Then(beforeGroupIncrement)
                .Then(context =>
                {
                    context.Saga.Count++;
                    context.Saga.Version++;
                }),
            When(ReadOnlyChecked)
                .Then(context =>
                {
                    context.Saga.Count += 100;
                    context.Saga.Version++;
                })
                .Finalize(),
            When(Finished).Finalize());
        SetCompletedWhenFinalized();
    }

    public State Running { get; private set; }
    public Event<ParityBegin> Began { get; private set; }
    public Event<ParityWarmUp> WarmedUp { get; private set; }
    public Event<ParityStart> Started { get; private set; }
    public Event<ParityFinish> Finished { get; private set; }
    public Event<ParityCompleteImmediately> CompletedImmediately { get; private set; }
    public Event<ParityIncrement> Incremented { get; private set; }
    public Event<ParityReadOnly> ReadOnlyChecked { get; private set; }
    public Event<ParityGroupIncrement> GroupIncremented { get; private set; }
}

class ParityReceiveObserver : IReceiveObserver
{
    readonly Guid _messageId;
    readonly TaskCompletionSource<bool> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ParityReceiveObserver(Guid messageId)
    {
        _messageId = messageId;
    }

    public Task<bool> Completed => _completed.Task;
    public Exception Exception { get; private set; }
    public bool IsDelivered { get; private set; }

    public Task PreReceive(ReceiveContext context) => Task.CompletedTask;

    public Task PostReceive(ReceiveContext context)
    {
        if (context.GetMessageId() == _messageId)
        {
            IsDelivered = context.IsDelivered;
            _completed.TrySetResult(context.IsFaulted);
        }
        return Task.CompletedTask;
    }

    public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
        where T : class => Task.CompletedTask;

    public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
        where T : class
    {
        if (context.MessageId == _messageId)
            Exception = exception;
        return Task.CompletedTask;
    }

    public Task ReceiveFault(ReceiveContext context, Exception exception)
    {
        if (context.GetMessageId() == _messageId)
        {
            Exception = exception;
            _completed.TrySetResult(true);
        }
        return Task.CompletedTask;
    }
}

public class ParityBegin : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
    public bool Fail { get; set; }
}

public class ParityWarmUp : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
    public string Name { get; set; }
    public string Group { get; set; }
}

public class ParityStart : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}

public class ParityFinish : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}

public class ParityCompleteImmediately : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}

public class ParityIncrement : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
    public bool Synchronize { get; set; }
    public bool HoldLock { get; set; }
    public bool Probe { get; set; }
}

public class ParityReadOnly : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}

public class ParityGroupIncrement
{
    public string Group { get; set; }
    public bool FailOnSecond { get; set; }
}

/// <summary>
/// Records which saga instances were inserted by a completed or failed SaveChanges
/// </summary>
class ParitySaveObserver : SaveChangesInterceptor
{
    readonly ConcurrentDictionary<DbContext, Guid[]> _pending = new();

    public ConcurrentDictionary<Guid, bool> SavedInserts { get; } = new();
    public ConcurrentDictionary<Guid, bool> FailedInserts { get; } = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _pending[eventData.Context] = eventData.Context.ChangeTracker.Entries<ParitySaga>()
            .Where(x => x.State == EntityState.Added)
            .Select(x => x.Entity.CorrelationId)
            .ToArray();

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Record(eventData.Context, SavedInserts);

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Record(eventData.Context, FailedInserts);

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    void Record(DbContext context, ConcurrentDictionary<Guid, bool> inserts)
    {
        if (!_pending.TryRemove(context, out Guid[] ids))
            return;

        foreach (var id in ids)
            inserts[id] = true;
    }
}
