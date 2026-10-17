namespace MessageTransit.RedisIntegration.Tests;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Configuration;
using NUnit.Framework;
using Saga;
using Serialization;
using StackExchange.Redis;
using TestFramework;
using Testing;


[TestFixture]
[Category("Integration")]
public class The_redis_client
{
    [Test]
    public void Should_negotiate_resp3_by_default()
    {
        var server = _multiplexer.GetServers().Single();

        Assert.That(server.Protocol, Is.EqualTo(RedisProtocol.Resp3));
    }

    [Test]
    public async Task Should_keep_a_saga_lock_exclusive_until_the_owner_releases_it()
    {
        var correlationId = NewId.NextGuid();

        var owner = CreateContext();
        await owner.Lock(correlationId, CancellationToken.None);

        var contender = CreateContext();
        Assert.That(async () => await contender.Lock(correlationId, CancellationToken.None), Throws.InstanceOf<MessageTransitException>());

        await owner.DisposeAsync();

        await contender.Lock(correlationId, CancellationToken.None);
        await contender.DisposeAsync();

        Assert.That(await _database.KeyExistsAsync(_options.FormatLockKey(correlationId)), Is.False);
    }

    [Test]
    public async Task Should_not_release_a_saga_lock_held_by_another_owner()
    {
        var correlationId = NewId.NextGuid();

        var owner = CreateContext();
        await owner.Lock(correlationId, CancellationToken.None);

        var released = await _database.LockReleaseAsync(_options.FormatLockKey(correlationId), "another-owner");

        Assert.Multiple(async () =>
        {
            Assert.That(released, Is.False);
            Assert.That(await _database.KeyExistsAsync(_options.FormatLockKey(correlationId)), Is.True);
        });

        await owner.DisposeAsync();
    }

    [Test]
    public async Task Should_expire_an_abandoned_saga_lock_after_the_lock_timeout()
    {
        var correlationId = NewId.NextGuid();
        var options = CreateOptions(lockTimeout: TimeSpan.FromSeconds(1));

        var abandoned = new RedisDatabaseContext<SimpleSaga>(_database, options);
        await abandoned.Lock(correlationId, CancellationToken.None);

        var timeToLive = await _database.KeyTimeToLiveAsync(options.FormatLockKey(correlationId));
        Assert.That(timeToLive, Is.Not.Null.And.GreaterThan(TimeSpan.Zero).And.LessThanOrEqualTo(TimeSpan.FromSeconds(1)));

        await Task.Delay(TimeSpan.FromSeconds(1.5));

        var contender = new RedisDatabaseContext<SimpleSaga>(_database, options);
        await contender.Lock(correlationId, CancellationToken.None);
        await contender.DisposeAsync();
    }

    [Test]
    public async Task Should_apply_the_saga_expiry()
    {
        var expiring = new SimpleSaga { CorrelationId = NewId.NextGuid() };
        var persistent = new SimpleSaga { CorrelationId = NewId.NextGuid() };

        await new RedisDatabaseContext<SimpleSaga>(_database, CreateOptions(expiry: TimeSpan.FromMinutes(1)))
            .Insert(SystemTextJsonMessageSerializer.Instance, expiring);
        await CreateContext().Insert(SystemTextJsonMessageSerializer.Instance, persistent);

        var expiringTimeToLive = await _database.KeyTimeToLiveAsync(_options.FormatSagaKey(expiring.CorrelationId));
        var persistentTimeToLive = await _database.KeyTimeToLiveAsync(_options.FormatSagaKey(persistent.CorrelationId));

        Assert.Multiple(() =>
        {
            Assert.That(expiringTimeToLive, Is.Not.Null.And.GreaterThan(TimeSpan.Zero).And.LessThanOrEqualTo(TimeSpan.FromMinutes(1)));
            Assert.That(persistentTimeToLive, Is.Null);
        });
    }

    [Test]
    public async Task Should_stop_waiting_for_a_saga_lock_when_cancelled()
    {
        var correlationId = NewId.NextGuid();

        var owner = CreateContext();
        await owner.Lock(correlationId, CancellationToken.None);

        var contender = new RedisDatabaseContext<SimpleSaga>(_database,
            CreateOptions(retryPolicy: Retry.Interval(1000, TimeSpan.FromMilliseconds(50))));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        var stopwatch = Stopwatch.StartNew();

        Assert.That(async () => await contender.Lock(correlationId, cancellation.Token),
            Throws.InstanceOf<MessageTransitException>().Or.InstanceOf<OperationCanceledException>());
        Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));

        await owner.DisposeAsync();
    }

    [Test]
    public async Task Should_reconnect_after_the_server_drops_the_connection()
    {
        var saga = new SimpleSaga
        {
            CorrelationId = NewId.NextGuid(),
            Name = "before"
        };
        await CreateContext().Insert(SystemTextJsonMessageSerializer.Instance, saga);

        var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var restored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _multiplexer.ConnectionFailed += (_, args) =>
        {
            if (args.ConnectionType == ConnectionType.Interactive)
                failed.TrySetResult();
        };
        _multiplexer.ConnectionRestored += (_, args) =>
        {
            if (args.ConnectionType == ConnectionType.Interactive)
                restored.TrySetResult();
        };

        var adminOptions = ConfigurationOptions.Parse(TestRedis.Configuration);
        adminOptions.AllowAdmin = true;

        await using (var admin = await ConnectionMultiplexer.ConnectAsync(adminOptions))
        {
            var killed = await admin.GetServers().Single().ClientKillAsync(clientType: ClientType.Normal, skipMe: true);
            Assert.That(killed, Is.GreaterThan(0));
        }

        await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await restored.Task.WaitAsync(TimeSpan.FromSeconds(30));

        var loaded = await CreateContext().Load(SystemTextJsonMessageSerializer.Instance, saga.CorrelationId);

        Assert.That(loaded?.Name, Is.EqualTo("before"));
    }

    [Test]
    public async Task Should_close_factory_connections_on_shutdown()
    {
        var factory = new ConnectionMultiplexerFactory();

        var multiplexer = factory.GetConnectionMultiplexer(ConfigurationOptions.Parse(TestRedis.Configuration));
        Assert.That(multiplexer.IsConnected, Is.True);

        await factory.DisposeAsync();

        Assert.That(multiplexer.IsConnected, Is.False);
    }

    ConnectionMultiplexer _multiplexer;
    IDatabase _database;
    RedisSagaRepositoryOptions<SimpleSaga> _options;

    [SetUp]
    public async Task Setup()
    {
        _multiplexer = await ConnectionMultiplexer.ConnectAsync(TestRedis.Configuration);
        _database = _multiplexer.GetDatabase();
        _options = CreateOptions();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _multiplexer.DisposeAsync();
    }

    RedisDatabaseContext<SimpleSaga> CreateContext()
    {
        return new RedisDatabaseContext<SimpleSaga>(_database, _options);
    }

    static RedisSagaRepositoryOptions<SimpleSaga> CreateOptions(TimeSpan? lockTimeout = null, TimeSpan? expiry = null, IRetryPolicy retryPolicy = null)
    {
        return new RedisSagaRepositoryOptions<SimpleSaga>(ConcurrencyMode.Pessimistic, lockTimeout, null, "client-specs", null, null, expiry,
            retryPolicy ?? Retry.None);
    }
}


[TestFixture(ConcurrencyMode.Optimistic)]
[TestFixture(ConcurrencyMode.Pessimistic)]
[Category("Integration")]
public class Concurrent_updates_to_one_saga : InMemoryTestFixture
{
    const int Increments = 20;

    readonly ConcurrencyMode _concurrencyMode;
    readonly Lazy<ISagaRepository<CountingSaga>> _sagaRepository;
    ConnectionMultiplexer _multiplexer;

    public Concurrent_updates_to_one_saga(ConcurrencyMode concurrencyMode)
    {
        _concurrencyMode = concurrencyMode;
        _sagaRepository = new Lazy<ISagaRepository<CountingSaga>>(() =>
        {
            _multiplexer = ConnectionMultiplexer.Connect(TestRedis.Configuration);

            return RedisSagaRepository<CountingSaga>.Create(_ => _multiplexer, () => _multiplexer.GetDatabase(),
                _concurrencyMode == ConcurrencyMode.Optimistic, keyPrefix: $"concurrent-{_concurrencyMode}");
        });
    }

    [Test]
    public async Task Should_apply_every_update()
    {
        var correlationId = NewId.NextGuid();

        await InputQueueSendEndpoint.Send(new StartCounting { CorrelationId = correlationId });

        Guid? found = await _sagaRepository.Value.ShouldContainSaga(correlationId, TestTimeout);
        Assert.That(found, Is.Not.Null);

        await Task.WhenAll(Enumerable.Range(0, Increments)
            .Select(_ => InputQueueSendEndpoint.Send(new IncrementCount { CorrelationId = correlationId })));

        found = await _sagaRepository.Value.ShouldContainSaga(correlationId, x => x is { Count: Increments }, TestTimeout);
        Assert.That(found, Is.Not.Null);

        var loaded = await ((ILoadSagaRepository<CountingSaga>)_sagaRepository.Value).Load(correlationId);
        Assert.That(loaded.Count, Is.EqualTo(Increments));
    }

    [OneTimeTearDown]
    public async Task CloseConnection()
    {
        if (_multiplexer != null)
            await _multiplexer.DisposeAsync();
    }

    protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
    {
        configurator.ConcurrentMessageLimit = 16;
        configurator.UseMessageRetry(r => r.Intervals(Enumerable.Repeat(10, 100).ToArray()));
        configurator.Saga(_sagaRepository.Value);
    }
}


public class CountingSaga : InitiatedBy<StartCounting>, Orchestrates<IncrementCount>, ISagaVersion
{
    public int Count { get; set; }

    public Task Consume(ConsumeContext<StartCounting> context)
    {
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<IncrementCount> context)
    {
        Count++;

        return Task.CompletedTask;
    }

    public Guid CorrelationId { get; set; }
    public int Version { get; set; }
}


public class StartCounting : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}


public class IncrementCount : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}
