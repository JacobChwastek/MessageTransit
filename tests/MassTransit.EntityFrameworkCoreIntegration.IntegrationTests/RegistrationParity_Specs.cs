namespace MassTransit.EntityFrameworkCoreIntegration.Tests;

using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MassTransit.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shared;


[TestFixture]
public class RegistrationParity_Specs
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_register_consume_load_and_query_repositories(bool globalProvider)
    {
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            if (globalProvider)
            {
                x.SetEntityFrameworkSagaRepositoryProvider(r => r.DatabaseFactory(() => new TrackingContext(CreateOptions())));
                x.AddSaga(typeof(SagaState));
            }
            else
                x.AddSaga<SagaState>().EntityFrameworkRepository(r => r.DatabaseFactory(() => new TrackingContext(CreateOptions())));

            x.UsingInMemory();
        });
        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();

        Assert.Multiple(() =>
        {
            Assert.That(scope.ServiceProvider.GetRequiredService<ISagaRepository<SagaState>>(), Is.Not.Null);
            Assert.That(scope.ServiceProvider.GetRequiredService<ILoadSagaRepository<SagaState>>(), Is.Not.Null);
            Assert.That(scope.ServiceProvider.GetRequiredService<IQuerySagaRepository<SagaState>>(), Is.Not.Null);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Should_leave_registered_context_disposal_to_its_scope(bool existingContext)
    {
        var services = new ServiceCollection();
        if (existingContext)
            services.AddDbContext<TrackingContext>(ConfigureOptions);
        services.AddMassTransit(x =>
        {
            x.AddSaga<SagaState>().EntityFrameworkRepository(r =>
            {
                if (existingContext)
                    r.ExistingDbContext<TrackingContext>();
                else
                    r.AddDbContext<TrackingContext, TrackingContext>((_, options) => ConfigureOptions(options));
            });
            x.UsingInMemory();
        });
        await using var provider = services.BuildServiceProvider(true);
        TrackingContext context;
        await using (var scope = provider.CreateAsyncScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<ISagaDbContextFactory<SagaState>>();
            context = (TrackingContext)factory.Create();
            Assert.That(context, Is.SameAs(scope.ServiceProvider.GetRequiredService<TrackingContext>()));
            Assert.That(factory.CreateScoped<Start>(null), Is.SameAs(context));
            await factory.ReleaseAsync(context);
            Assert.That(context.Disposed, Is.False);

            await using var otherScope = provider.CreateAsyncScope();
            Assert.That(otherScope.ServiceProvider.GetRequiredService<ISagaDbContextFactory<SagaState>>().Create(), Is.Not.SameAs(context));
        }

        Assert.That(context.Disposed, Is.True);
    }

    [Test]
    public async Task Should_create_and_release_each_delegate_context()
    {
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.AddSaga<SagaState>().EntityFrameworkRepository(r => r.DatabaseFactory(_ => () => new TrackingContext(CreateOptions())));
            x.UsingInMemory();
        });
        await using var provider = services.BuildServiceProvider(true);
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<ISagaDbContextFactory<SagaState>>();
        var first = (TrackingContext)factory.Create();
        var second = (TrackingContext)factory.CreateScoped<Start>(null);

        Assert.That(second, Is.Not.SameAs(first));
        await factory.ReleaseAsync(first);
        await factory.ReleaseAsync(second);
        Assert.Multiple(() =>
        {
            Assert.That(first.Disposed, Is.True);
            Assert.That(second.Disposed, Is.True);
        });
    }

    [TestCase(ConcurrencyMode.Pessimistic, null, IsolationLevel.Serializable)]
    [TestCase(ConcurrencyMode.Optimistic, null, IsolationLevel.ReadCommitted)]
    [TestCase(ConcurrencyMode.Optimistic, IsolationLevel.RepeatableRead, IsolationLevel.RepeatableRead)]
    public async Task Should_preserve_default_and_custom_isolation(ConcurrencyMode mode, IsolationLevel? configured, IsolationLevel expected)
    {
        var services = new ServiceCollection();
        services.AddMassTransit(x =>
        {
            x.AddSaga<SagaState>().EntityFrameworkRepository(r =>
            {
                r.DatabaseFactory(() => new TrackingContext(CreateOptions()));
                if (configured.HasValue)
                    r.IsolationLevel = configured.Value;
                r.ConcurrencyMode = mode;
            });
            x.UsingInMemory();
        });
        await using var provider = services.BuildServiceProvider(true);
        var strategy = provider.GetRequiredService<ISagaRepositoryLockStrategy<SagaState>>();

        Assert.Multiple(() =>
        {
            Assert.That(strategy.IsolationLevel, Is.EqualTo(expected));
            Assert.That(strategy.IsTransactionEnabled, Is.True);
        });
    }

    [Test]
    public void Should_reject_registration_without_a_context()
    {
        var configurator = new EntityFrameworkSagaRepositoryConfigurator<SagaState>();

        var result = configurator.Validate().Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Disposition, Is.EqualTo(ValidationResultDisposition.Failure));
            Assert.That(result.Key, Is.EqualTo("DbContext"));
            Assert.That(result.Message, Is.EqualTo("must be specified"));
        });
    }

    static DbContextOptions<TrackingContext> CreateOptions()
    {
        var builder = new DbContextOptionsBuilder<TrackingContext>();
        ConfigureOptions(builder);
        return builder.Options;
    }

    static void ConfigureOptions(DbContextOptionsBuilder options)
    {
        options.UseOfflineSqlServer();
    }


    public class TrackingContext : DbContext
    {
        public TrackingContext(DbContextOptions<TrackingContext> options) : base(options)
        {
        }

        public bool Disposed { get; private set; }

        public override async ValueTask DisposeAsync()
        {
            Disposed = true;
            await base.DisposeAsync();
        }
    }


    public class SagaState : ISaga, InitiatedBy<Start>
    {
        public Guid CorrelationId { get; set; }

        public Task Consume(ConsumeContext<Start> context) => Task.CompletedTask;
    }


    public class Start : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
    }
}
