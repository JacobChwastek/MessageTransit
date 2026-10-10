namespace MessageTransit.EntityFrameworkCoreIntegration.Audit;

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

public class AuditDbContext : DbContext
{
    protected AuditDbContext(string auditTableName, string auditTableSchema = null)
    {
        AuditTableName = auditTableName;
        AuditTableSchema = auditTableSchema;
    }

    public AuditDbContext(DbContextOptions options, string auditTableName, string auditTableSchema = null)
        : base(options)
    {
        AuditTableName = auditTableName;
        AuditTableSchema = auditTableSchema;
    }

    public string AuditTableName { get; }
    public string AuditTableSchema { get; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        var coreOptions = optionsBuilder.Options.FindExtension<CoreOptionsExtension>();
        var hasCustomModelCache = coreOptions?.ReplacedServices?.Keys.Any(key => key.Item1 == typeof(IModelCacheKeyFactory)) ?? false;

        // pooled contexts have frozen options and a fixed table per context type, so the default model cache applies
        var isPooled = coreOptions?.MaxPoolSize != null;

        if (coreOptions?.InternalServiceProvider == null && !hasCustomModelCache && !isPooled)
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, AuditModelCacheKeyFactory>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditMapping(AuditTableName, AuditTableSchema));
    }

    /// <summary>
    /// Throws when the cached EF model maps audit records to a different table or schema than this context was created with
    /// </summary>
    public void EnsureAuditModel()
    {
        var entityType = Model.FindEntityType(typeof(AuditRecord));
        var expectedSchema = string.IsNullOrWhiteSpace(AuditTableSchema) ? Model.GetDefaultSchema() : AuditTableSchema;

        var tableName = entityType?.GetTableName();
        var schema = entityType?.GetSchema();

        if (tableName == AuditTableName && schema == expectedSchema)
            return;

        throw new InvalidOperationException($"The audit model maps to {Format(schema, tableName)} instead of {Format(expectedSchema, AuditTableName)}. "
            + $"When the options supply an internal service provider or replace {nameof(IModelCacheKeyFactory)}, the model cache key must include "
            + $"the audit table and schema, for example by using {nameof(AuditModelCacheKeyFactory)}.");
    }

    static string Format(string schema, string table)
    {
        return string.IsNullOrEmpty(schema) ? table : $"{schema}.{table}";
    }
}
