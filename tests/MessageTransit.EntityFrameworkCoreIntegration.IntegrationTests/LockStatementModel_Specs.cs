namespace MessageTransit.EntityFrameworkCoreIntegration.Tests;

using System;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shared;


[TestFixture]
public class LockStatementModel_Specs
{
    [TestCase(false, false, "SELECT * FROM [message_schema].saga_state WITH (UPDLOCK, ROWLOCK) WHERE correlation_id = @p0")]
    [TestCase(false, true, "SELECT * FROM [message_schema].saga_state WITH (UPDLOCK, ROWLOCK) WHERE correlation_id = @p0 AND version = @p1")]
    [TestCase(true, false, "SELECT *, xmin FROM \"message_schema\".\"saga_state\" WHERE \"correlation_id\" = @p0 FOR UPDATE")]
    [TestCase(true, true, "SELECT *, xmin FROM \"message_schema\".\"saga_state\" WHERE \"correlation_id\" = @p0 AND \"version\" = @p1 FOR UPDATE")]
    public void Should_use_the_provider_model_schema_table_and_column_names(bool postgres, bool composite, string expected)
    {
        var options = new DbContextOptionsBuilder<ModelContext>();
        if (postgres)
            options.UseOfflineNpgsql();
        else
            options.UseOfflineSqlServer();

        using var context = new ModelContext(options.Options);
        ILockStatementProvider provider = postgres
            ? new PostgresLockStatementProvider(enableSchemaCaching: false)
            : new SqlServerLockStatementProvider(enableSchemaCaching: false);

        var statement = composite
            ? provider.GetRowLockStatement<SagaState>(context, nameof(SagaState.CorrelationId), nameof(SagaState.Version))
            : provider.GetRowLockStatement<SagaState>(context);

        Assert.That(statement, Is.EqualTo(expected));
    }


    class SagaState : ISaga
    {
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
    }


    class ModelContext : DbContext
    {
        public ModelContext(DbContextOptions<ModelContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<SagaState>();
            entity.ToTable("saga_state", "message_schema");
            entity.HasKey(x => x.CorrelationId);
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            entity.Property(x => x.Version).HasColumnName("version");
        }
    }
}
