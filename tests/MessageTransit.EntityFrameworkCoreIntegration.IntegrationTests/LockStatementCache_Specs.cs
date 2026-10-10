namespace MessageTransit.EntityFrameworkCoreIntegration.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using EntityFrameworkCoreIntegration.Saga;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shared;


[TestFixture]
public class LockStatementCache_Specs
{
    [TestCase(true)]
    [TestCase(false)]
    public void Should_use_each_context_model_for_the_same_saga_type(bool enableSchemaCaching)
    {
        using var first = new FirstContext<ModelScenario>(SqlServerOptions<FirstContext<ModelScenario>>());
        using var second = new SecondContext<ModelScenario>(SqlServerOptions<SecondContext<ModelScenario>>());
        var provider = new SqlServerLockStatementProvider(enableSchemaCaching);

        Assert.That(provider.GetRowLockStatement<SagaState<ModelScenario>>(first),
            Is.EqualTo("SELECT * FROM [first_schema].first_state WITH (UPDLOCK, ROWLOCK) WHERE first_id = @p0"));
        Assert.That(provider.GetRowLockStatement<SagaState<ModelScenario>>(second),
            Is.EqualTo("SELECT * FROM [second_schema].second_state WITH (UPDLOCK, ROWLOCK) WHERE second_id = @p0"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Should_use_each_provider_model_for_the_same_saga_type(bool enableSchemaCaching)
    {
        using var sqlServer = new ProviderContext<ProviderScenario>(SqlServerOptions<ProviderContext<ProviderScenario>>());
        using var postgres = new ProviderContext<ProviderScenario>(new DbContextOptionsBuilder<ProviderContext<ProviderScenario>>()
            .UseOfflineNpgsql().Options);

        Assert.That(new SqlServerLockStatementProvider(enableSchemaCaching).GetRowLockStatement<SagaState<ProviderScenario>>(sqlServer),
            Is.EqualTo("SELECT * FROM [sql_schema].sql_state WITH (UPDLOCK, ROWLOCK) WHERE sql_id = @p0"));
        Assert.That(new PostgresLockStatementProvider(enableSchemaCaching).GetRowLockStatement<SagaState<ProviderScenario>>(postgres),
            Is.EqualTo("SELECT *, xmin FROM \"pg_schema\".\"pg_state\" WHERE \"pg_id\" = @p0 FOR UPDATE"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Should_cache_the_requested_property_list_in_order(bool enableSchemaCaching)
    {
        using var context = new FirstContext<PropertyScenario>(SqlServerOptions<FirstContext<PropertyScenario>>());
        var provider = new SqlServerLockStatementProvider(enableSchemaCaching);

        Assert.That(provider.GetRowLockStatement<SagaState<PropertyScenario>>(context),
            Is.EqualTo("SELECT * FROM [first_schema].first_state WITH (UPDLOCK, ROWLOCK) WHERE first_id = @p0"));
        Assert.That(provider.GetRowLockStatement<SagaState<PropertyScenario>>(context, "CorrelationId", "Version"),
            Is.EqualTo("SELECT * FROM [first_schema].first_state WITH (UPDLOCK, ROWLOCK) WHERE first_id = @p0 AND version = @p1"));
        Assert.That(provider.GetRowLockStatement<SagaState<PropertyScenario>>(context, "Version", "CorrelationId"),
            Is.EqualTo("SELECT * FROM [first_schema].first_state WITH (UPDLOCK, ROWLOCK) WHERE version = @p0 AND first_id = @p1"));
        Assert.That(provider.GetRowLockStatement<SagaState<PropertyScenario>>(context, "Version"),
            Is.EqualTo("SELECT * FROM [first_schema].first_state WITH (UPDLOCK, ROWLOCK) WHERE version = @p0"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Should_preserve_each_requested_property_order(bool enableSchemaCaching)
    {
        using var context = new FirstContext<OrderScenario>(SqlServerOptions<FirstContext<OrderScenario>>());
        var provider = new SqlServerLockStatementProvider(enableSchemaCaching);
        var properties = new[] { "CorrelationId", "Version" };

        Assert.That(provider.GetRowLockStatement<SagaState<OrderScenario>>(context, properties),
            Is.EqualTo("SELECT * FROM [first_schema].first_state WITH (UPDLOCK, ROWLOCK) WHERE first_id = @p0 AND version = @p1"));

        properties[0] = "Version";
        properties[1] = "CorrelationId";
        Assert.That(provider.GetRowLockStatement<SagaState<OrderScenario>>(context, properties),
            Is.EqualTo("SELECT * FROM [first_schema].first_state WITH (UPDLOCK, ROWLOCK) WHERE version = @p0 AND first_id = @p1"));
        Assert.That(provider.GetRowLockStatement<SagaState<OrderScenario>>(context, "CorrelationId", "Version"),
            Is.EqualTo("SELECT * FROM [first_schema].first_state WITH (UPDLOCK, ROWLOCK) WHERE first_id = @p0 AND version = @p1"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Should_apply_each_providers_fallback_schema(bool enableSchemaCaching)
    {
        using var context = new DefaultSchemaContext<DefaultSchemaScenario>(SqlServerOptions<DefaultSchemaContext<DefaultSchemaScenario>>());

        Assert.That(new SqlServerLockStatementProvider("first_schema", enableSchemaCaching)
                .GetRowLockStatement<SagaState<DefaultSchemaScenario>>(context),
            Is.EqualTo("SELECT * FROM [first_schema].default_state WITH (UPDLOCK, ROWLOCK) WHERE CorrelationId = @p0"));
        Assert.That(new SqlServerLockStatementProvider("second_schema", enableSchemaCaching)
                .GetRowLockStatement<SagaState<DefaultSchemaScenario>>(context),
            Is.EqualTo("SELECT * FROM [second_schema].default_state WITH (UPDLOCK, ROWLOCK) WHERE CorrelationId = @p0"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Should_resolve_outbox_columns_and_each_providers_fallback_schema(bool enableSchemaCaching)
    {
        using var context = new OutboxContext(SqlServerOptions<OutboxContext>());
        var firstProvider = new SqlServerLockStatementProvider("first_schema", enableSchemaCaching);
        var secondProvider = new SqlServerLockStatementProvider("second_schema", enableSchemaCaching);

        Assert.That(firstProvider.GetRowLockStatement<OutboxState>(context, nameof(OutboxState.LockId)),
            Is.EqualTo("SELECT * FROM [first_schema].default_outbox WITH (UPDLOCK, ROWLOCK) WHERE LockId = @p0"));
        Assert.That(firstProvider.GetOutboxStatement(context),
            Is.EqualTo("SELECT TOP 1 * FROM [first_schema].default_outbox WITH (UPDLOCK, ROWLOCK, READPAST) ORDER BY created_at"));
        Assert.That(secondProvider.GetOutboxStatement(context),
            Is.EqualTo("SELECT TOP 1 * FROM [second_schema].default_outbox WITH (UPDLOCK, ROWLOCK, READPAST) ORDER BY created_at"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Should_resolve_the_pessimistic_load_statement_for_each_context(bool enableSchemaCaching)
    {
        using var first = new FirstContext<ExecutorScenario>(SqlServerOptions<FirstContext<ExecutorScenario>>());
        using var second = new SecondContext<ExecutorScenario>(SqlServerOptions<SecondContext<ExecutorScenario>>());
        var statements = new List<string>();
        var executor = new PessimisticLoadQueryExecutor<SagaState<ExecutorScenario>>(
            new SqlServerLockStatementProvider(enableSchemaCaching), query =>
            {
                statements.Add(query.ToQueryString());
                throw new QueryCapturedException();
            });

        Assert.Throws<QueryCapturedException>(() => executor.Load(first, Guid.NewGuid(), CancellationToken.None));
        Assert.Throws<QueryCapturedException>(() => executor.Load(second, Guid.NewGuid(), CancellationToken.None));
        Assert.Throws<QueryCapturedException>(() => executor.Load(first, Guid.NewGuid(), CancellationToken.None));

        Assert.That(statements[0], Does.Contain("[first_schema].first_state"));
        Assert.That(statements[1], Does.Contain("[second_schema].second_state"));
        Assert.That(statements[2], Does.Contain("[first_schema].first_state"));
    }

    static DbContextOptions<TContext> SqlServerOptions<TContext>()
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>().UseOfflineSqlServer().Options;
    }


    class SagaState<TScenario> : ISaga
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
    }


    class FirstContext<TScenario>(DbContextOptions<FirstContext<TScenario>> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<SagaState<TScenario>>();
            entity.ToTable("first_state", "first_schema");
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CorrelationId).HasColumnName("first_id");
            entity.Property(x => x.Version).HasColumnName("version");
        }
    }


    class SecondContext<TScenario>(DbContextOptions<SecondContext<TScenario>> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<SagaState<TScenario>>();
            entity.ToTable("second_state", "second_schema");
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CorrelationId).HasColumnName("second_id");
        }
    }


    class ProviderContext<TScenario>(DbContextOptions<ProviderContext<TScenario>> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var prefix = Database.IsNpgsql() ? "pg" : "sql";
            var entity = modelBuilder.Entity<SagaState<TScenario>>();
            entity.ToTable($"{prefix}_state", $"{prefix}_schema");
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CorrelationId).HasColumnName($"{prefix}_id");
        }
    }


    class DefaultSchemaContext<TScenario>(DbContextOptions<DefaultSchemaContext<TScenario>> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<SagaState<TScenario>>();
            entity.ToTable("default_state");
            entity.HasKey(x => x.CorrelationId);
        }
    }


    class OutboxContext(DbContextOptions<OutboxContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<OutboxState>();
            entity.ToTable("default_outbox");
            entity.HasKey(x => x.OutboxId);
            entity.Property(x => x.Created).HasColumnName("created_at");
        }
    }


    class ModelScenario
    {
    }
    class ProviderScenario
    {
    }
    class PropertyScenario
    {
    }
    class OrderScenario
    {
    }
    class DefaultSchemaScenario
    {
    }
    class ExecutorScenario
    {
    }
    class QueryCapturedException : Exception
    {
    }
}
