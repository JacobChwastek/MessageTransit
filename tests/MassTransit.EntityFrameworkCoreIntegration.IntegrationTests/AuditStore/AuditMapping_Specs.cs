namespace MassTransit.EntityFrameworkCoreIntegration.Tests.AuditStore;

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Audit;
using MassTransit.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shared;


[TestFixture]
public class AuditMapping_Specs
{
    [Test]
    public void Should_preserve_an_application_supplied_model_cache_factory()
    {
        var options = new DbContextOptionsBuilder()
            .UseOfflineSqlServer()
            .ReplaceService<IModelCacheKeyFactory, ApplicationAuditModelCacheKeyFactory>()
            .Options;
        using var context = new AuditDbContext(options, "application_audit");

        Assert.That(context.GetService<IModelCacheKeyFactory>(), Is.TypeOf<ApplicationAuditModelCacheKeyFactory>());
    }

    [Test]
    public void Should_accept_an_application_supplied_internal_service_provider()
    {
        using var services = CreateInternalServices();
        var options = InternalServiceProviderOptions(services);
        using var context = new AuditDbContext(options, "external_audit", "external_schema");

        var entity = context.Model.FindEntityType(typeof(AuditRecord));

        Assert.That(entity.GetTableName(), Is.EqualTo("external_audit"));
        Assert.That(entity.GetSchema(), Is.EqualTo("external_schema"));
        Assert.DoesNotThrow(() => context.EnsureAuditModel());
    }

    [Test]
    public void Should_reject_a_store_whose_cached_audit_model_maps_another_table_before_database_access()
    {
        using var services = CreateInternalServices();
        var options = new DbContextOptionsBuilder()
            .UseOfflineSqlServer()
            .UseInternalServiceProvider(services)
            .AddInterceptors(new RejectConnection())
            .Options;
        using (var first = new AuditDbContext(options, "first_audit"))
            first.EnsureAuditModel();

        IMessageAuditStore store = new EntityFrameworkAuditStore(options, "second_audit", "second_schema");

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await store.StoreMessage(new AuditedMessage(), new MessageAuditMetadata()));
        Assert.That(exception.Message, Does.Contain("first_audit").And.Contain("second_schema.second_audit"));
    }

    [Test]
    public void Should_allow_external_registration_of_the_audit_model_cache()
    {
        using var services = CreateInternalServices(registerAuditModelCache: true);
        var options = InternalServiceProviderOptions(services);
        using var first = new AuditDbContext(options, "audit", "first_schema");
        using var second = new AuditDbContext(options, "audit", "second_schema");

        Assert.That(first.Model.FindEntityType(typeof(AuditRecord)).GetSchema(), Is.EqualTo("first_schema"));
        Assert.That(second.Model.FindEntityType(typeof(AuditRecord)).GetSchema(), Is.EqualTo("second_schema"));
        Assert.That(second.Model, Is.Not.SameAs(first.Model));
        Assert.DoesNotThrow(() => second.EnsureAuditModel());
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Should_keep_each_audit_stores_table_and_schema(bool postgres)
    {
        var options = new DbContextOptionsBuilder();
        if (postgres)
            options.UseOfflineNpgsql();
        else
            options.UseOfflineSqlServer();

        // the same table name in every store, so only the schema can tell the mappings apart
        using var first = new AuditDbContext(options.Options, "audit", "first_schema");
        using var second = new AuditDbContext(options.Options, "audit", "second_schema");
        using var defaultSchema = new AuditDbContext(options.Options, "audit");
        using var firstAgain = new AuditDbContext(options.Options, "audit", "first_schema");

        var firstEntity = first.Model.FindEntityType(typeof(AuditRecord));
        var secondEntity = second.Model.FindEntityType(typeof(AuditRecord));
        var defaultEntity = defaultSchema.Model.FindEntityType(typeof(AuditRecord));

        Assert.Multiple(() =>
        {
            Assert.That(firstEntity.GetTableName(), Is.EqualTo("audit"));
            Assert.That(firstEntity.GetSchema(), Is.EqualTo("first_schema"));
            Assert.That(secondEntity.GetTableName(), Is.EqualTo("audit"));
            Assert.That(secondEntity.GetSchema(), Is.EqualTo("second_schema"));
            Assert.That(defaultEntity.GetTableName(), Is.EqualTo("audit"));
            Assert.That(defaultEntity.GetSchema(), Is.Null);
            Assert.That(second.Model, Is.Not.SameAs(first.Model));
            Assert.That(defaultSchema.Model, Is.Not.SameAs(first.Model).And.Not.SameAs(second.Model));
            Assert.That(firstAgain.Model, Is.SameAs(first.Model));
            Assert.DoesNotThrow(() => second.EnsureAuditModel());
            Assert.DoesNotThrow(() => defaultSchema.EnsureAuditModel());
        });
    }

    [Test]
    public void Should_support_pooled_audit_contexts()
    {
        var options = new DbContextOptionsBuilder<PooledAuditDbContext>()
            .UseOfflineSqlServer()
            .Options;
        var factory = new PooledDbContextFactory<PooledAuditDbContext>(options);

        using var context = factory.CreateDbContext();

        Assert.That(context.Model.FindEntityType(typeof(AuditRecord)).GetTableName(), Is.EqualTo("pooled_audit"));
        Assert.DoesNotThrow(() => context.EnsureAuditModel());
    }

    [Test]
    public void Should_distinguish_design_time_and_runtime_models()
    {
        var options = new DbContextOptionsBuilder().UseOfflineSqlServer().Options;
        using var context = new AuditDbContext(options, "audit");
        var factory = context.GetService<IModelCacheKeyFactory>();

        Assert.That(factory.Create(context, designTime: true), Is.Not.EqualTo(factory.Create(context, designTime: false)));
    }


    static ServiceProvider CreateInternalServices(bool registerAuditModelCache = false)
    {
        var services = new ServiceCollection().AddEntityFrameworkSqlServer();
        if (registerAuditModelCache)
            services.AddSingleton<IModelCacheKeyFactory, AuditModelCacheKeyFactory>();

        return services.BuildServiceProvider();
    }

    static DbContextOptions InternalServiceProviderOptions(IServiceProvider services)
    {
        return new DbContextOptionsBuilder().UseOfflineSqlServer().UseInternalServiceProvider(services).Options;
    }


    public class ApplicationAuditModelCacheKeyFactory : AuditModelCacheKeyFactory
    {
    }


    public class AuditedMessage
    {
    }


    class RejectConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            throw new AssertionException("The audit store opened a database connection before validating its model");
        }

        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            throw new AssertionException("The audit store opened a database connection before validating its model");
        }
    }


    public class PooledAuditDbContext(DbContextOptions<PooledAuditDbContext> options) : AuditDbContext(options, "pooled_audit");
}
