namespace MessageTransit.EntityFrameworkCoreIntegration;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;


public class SqlLockStatementProvider : ILockStatementProvider
{
    static readonly string[] CorrelationIdProperty = [nameof(ISaga.CorrelationId)];
    static readonly string[] OutboxProperty = [nameof(OutboxState.Created)];

    readonly bool _enableSchemaCaching;
    readonly ILockStatementFormatter _formatter;
    readonly ConditionalWeakTable<IModel, ConcurrentDictionary<StatementKey, string>> _statements = new();

    public SqlLockStatementProvider(string defaultSchema, ILockStatementFormatter formatter, bool enableSchemaCaching = true)
    {
        DefaultSchema = defaultSchema;

        _formatter = formatter;
        _enableSchemaCaching = enableSchemaCaching;
    }

    public SqlLockStatementProvider(ILockStatementFormatter formatter, bool enableSchemaCaching = true)
    {
        _formatter = formatter;
        _enableSchemaCaching = enableSchemaCaching;
    }

    string DefaultSchema { get; }

    public virtual string GetRowLockStatement<T>(DbContext context) where T : class
    {
        return GetStatement(context, new StatementKey(typeof(T), CorrelationIdProperty, false));
    }

    public virtual string GetRowLockStatement<T>(DbContext context, params string[] propertyNames) where T : class
    {
        return GetStatement(context, new StatementKey(typeof(T), propertyNames, false));
    }

    public virtual string GetOutboxStatement(DbContext context)
    {
        return GetStatement(context, new StatementKey(typeof(OutboxState), OutboxProperty, true));
    }

    string GetStatement(DbContext context, StatementKey key)
    {
        var model = context.Model;

        if (!_enableSchemaCaching)
            return FormatStatement(model, key);

        ConcurrentDictionary<StatementKey, string> statements = _statements.GetValue(model, _ => new ConcurrentDictionary<StatementKey, string>());
        if (statements.TryGetValue(key, out var statement))
            return statement;

        // the caller owns the property array, so the stored key keeps its own copy
        return statements.GetOrAdd(key.Detach(), static (item, state) => state.Provider.FormatStatement(state.Model, item), (Provider: this, Model: model));
    }

    string FormatStatement(IModel model, StatementKey key)
    {
        var schemaTableTrio = ReadModelMetadata(model, key.EntityType, key.PropertyNames);
        var schema = schemaTableTrio.Schema ?? DefaultSchema;

        var sb = new StringBuilder(128);

        if (key.IsOutbox)
            _formatter.CreateOutboxStatement(sb, schema, schemaTableTrio.Table, schemaTableTrio.ColumnNames[0]);
        else
        {
            _formatter.Create(sb, schema, schemaTableTrio.Table);

            for (var i = 0; i < key.PropertyNames.Length; i++)
                _formatter.AppendColumn(sb, i, schemaTableTrio.ColumnNames[i]);

            _formatter.Complete(sb);
        }

        return sb.ToString();
    }

    static SchemaTableColumnTrio ReadModelMetadata(IModel model, Type type, string[] propertyNames)
    {
        var entityType = model.FindEntityType(type)
            ?? throw new InvalidOperationException($"Entity type not found: {TypeCache.GetShortName(type)}");

        var schema = entityType.GetSchema();
        var tableName = entityType.GetTableName();

        var columnNames = new List<string>();

        for (var i = 0; i < propertyNames.Length; i++)
        {
            var property = entityType.GetProperties().Single(x => x.Name.Equals(propertyNames[i], StringComparison.OrdinalIgnoreCase));

            var storeObjectIdentifier = StoreObjectIdentifier.Table(tableName, schema);
            var columnName = property.GetColumnName(storeObjectIdentifier);

            columnNames.Add(columnName);
        }

        if (string.IsNullOrWhiteSpace(tableName))
            throw new MessageTransitException($"Unable to determine saga table name: {TypeCache.GetShortName(type)} (using model metadata).");

        return new SchemaTableColumnTrio(schema, tableName, columnNames.ToArray());
    }


    readonly struct StatementKey : IEquatable<StatementKey>
    {
        public StatementKey(Type entityType, string[] propertyNames, bool isOutbox)
        {
            EntityType = entityType;
            PropertyNames = propertyNames;
            IsOutbox = isOutbox;
        }

        public Type EntityType { get; }
        public string[] PropertyNames { get; }
        public bool IsOutbox { get; }

        public StatementKey Detach()
        {
            return new StatementKey(EntityType, PropertyNames.ToArray(), IsOutbox);
        }

        public bool Equals(StatementKey other)
        {
            return EntityType == other.EntityType && IsOutbox == other.IsOutbox
                && PropertyNames.SequenceEqual(other.PropertyNames, StringComparer.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is StatementKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(EntityType);
            hash.Add(IsOutbox);
            foreach (var propertyName in PropertyNames)
                hash.Add(propertyName, StringComparer.OrdinalIgnoreCase);

            return hash.ToHashCode();
        }
    }


    protected readonly struct SchemaTableColumnTrio
    {
        public SchemaTableColumnTrio(string schema, string table, string[] columnNames)
        {
            Schema = schema;
            Table = table;
            ColumnNames = columnNames;
        }

        public readonly string Schema;
        public readonly string Table;
        public readonly string[] ColumnNames;
    }
}
