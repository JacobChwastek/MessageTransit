# Migrating from Entity Framework 6 to EF Core

The EF6 adapter and its test project have been removed from this fork. EF Core is the retained Entity Framework integration and targets .NET 10. No `MessageTransit.EntityFramework` package is planned; the retained source currently builds as `MassTransit.EntityFrameworkCore`, with `MessageTransit.EntityFrameworkCore` as its [planned package identity](PACKAGE_IDENTITY.md#package-id-map).

This is a breaking removal for applications referencing the EF6 adapter. Replace that reference with the EF Core integration and port the application context, mappings, registration, and custom persistence code described below. EF Core provides saga and audit operations with different APIs. The examples use the namespaces currently present in the source tree.

Behavioral equivalence does not establish compatibility with an existing database. Review the generated model and migration SQL against a copy of the application database before switching adapters. Keep table names, schemas, correlation IDs, concurrency tokens, and serialized audit content explicit during that review.

## Behavior and regression coverage

| Area | EF6 behavior and EF Core equivalent | EF Core regression coverage |
| --- | --- | --- |
| Registration | Per-saga `EntityFrameworkRepository` and the global `SetEntityFrameworkSagaRepositoryProvider` register consume, load, and query repositories. A context factory or registered context is required. | [RegistrationParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/RegistrationParity_Specs.cs); [container integration](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/Container_Specs.cs). |
| Context ownership | Delegate factories create and dispose contexts per operation. Container factories leave disposal to the dependency injection scope. EF Core uses `ReleaseAsync` for custom factories. | [RegistrationParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/RegistrationParity_Specs.cs). See the `DatabaseFactory` lifetime difference below. |
| Saga identity and maps | `SagaClassMap<TSaga>` configures an application-assigned `CorrelationId` primary key and applies the derived entity map. EF Core also invokes `ConfigureCorrelationIdKey` before the entity configuration hook. | [SagaClassMap_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaClassMap_Specs.cs). |
| Optimistic concurrency | Configure a concurrency token in the saga map and message retry for conflicting updates. Default transaction isolation through `ConcurrencyMode.Optimistic` or the direct optimistic factory is `ReadCommitted`. A mode selection alone does not create a concurrency token. | [SagaBehaviorParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaBehaviorParity_Specs.cs); [registration isolation](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/RegistrationParity_Specs.cs). |
| Pessimistic concurrency | Load and update sagas inside a transaction with provider-specific locking. The default isolation is `Serializable`; registration accepts an explicit isolation level. | [SagaBehaviorParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaBehaviorParity_Specs.cs); [lock SQL mapping](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/LockStatementModel_Specs.cs). |
| Custom queries | Direct repository factories and `CustomizeQuery` accept a query transformation. EF Core supports `Include`/`ThenInclude` and forces tracked consume loads so changes can be saved. Public detached load/find operations have a separate path. | [nested dependency inclusion](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaWithDependency/Using_custom_include_in_repository.cs); [SagaBehaviorParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaBehaviorParity_Specs.cs). |
| Query correlation | Optimistic mode loads matching sagas through a LINQ predicate. Pessimistic mode first selects IDs, then locks and loads each instance; rows removed between selection and load are skipped. | [SagaBehaviorParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaBehaviorParity_Specs.cs). |
| Initiation and pre-insert | Ordinary initiation persists a new saga. `InsertOnInitial` persists before executing the behavior and falls back to loading the existing instance after a duplicate insert. | [saga locator](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SimpleSaga/SagaLocator_Specs.cs); [SagaBehaviorParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaBehaviorParity_Specs.cs). |
| Discard, completion, and read-only events | Missing-instance discard leaves no row. Completion removes persisted sagas; completing during initiation leaves no orphan. Read-only events leave existing scalar saga state unchanged. | [SagaBehaviorParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaBehaviorParity_Specs.cs); [ReadOnly_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/ReadOnly_Specs.cs). |
| Transaction atomicity | Successful processing commits. Processing exceptions roll back database changes, including a pre-insert or an explicit save performed before the failure. Database atomicity does not by itself make outgoing messages transactional. | [SagaBehaviorParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/SagaBehaviorParity_Specs.cs). |
| Cancellation | The default compiled optimistic consume query and the customized query both forward the cancellation token to EF Core. | [OptimisticLoadCancellation_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/OptimisticLoadCancellation_Specs.cs). |
| Audit storage | Send/consume audit observers write generated integer IDs, message metadata, addresses, headers, custom values, and JSON message bodies. EF Core additionally accepts an explicit audit schema and preserves `InputAddress`, which EF6's record factory omitted. | [observer audit counts](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/AuditStore/AuditStore_Specs.cs); [AuditRecordParity_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/AuditStore/AuditRecordParity_Specs.cs). |
| Model-specific SQL and audit tables | Lock statements are cached per provider instance, EF model, entity type, and ordered property list. Provider fallback schemas remain separate. Each audit table/schema combination gets its own EF model, and the audit store rejects a model cached for another table or schema. | [LockStatementCache_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/LockStatementCache_Specs.cs); [AuditMapping_Specs](tests/MassTransit.EntityFrameworkCoreIntegration.Tests/AuditStore/AuditMapping_Specs.cs). |

The retained implementation is in the [EF Core integration](src/Persistence/MassTransit.EntityFrameworkCoreIntegration). The [historical EF6 integration](https://github.com/JacobChwastek/MessageTransit/tree/17c9d4a7ca7d30606731fc64fa49c42464c10b2f/src/Persistence/MassTransit.EntityFrameworkIntegration) is available at the last commit before its removal. The regression links identify specific coverage; they do not imply that every provider, custom mapping, or concurrency interleaving has been tested.

## Context construction and lifetime

Replace `System.Data.Entity.DbContext` with `Microsoft.EntityFrameworkCore.DbContext` and configure the provider through `DbContextOptions`. EF6 constructors accepting an `ObjectContext`, EF6 compiled model, or connection string have no direct EF Core signature. Configure the connection or EF Core model on the options builder instead.

For a scoped application context:

```csharp
services.AddDbContext<ApplicationSagaDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<ApplicationSagaDbContext>();
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
            r.UseSqlServer();
        });

    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});
```

`ApplicationSagaDbContext`, `OrderStateMachine`, `OrderState`, and `connectionString` are application-defined. Select the application transport in its bus configuration.

An alternative is `r.AddDbContext<TContext, TImplementation>((provider, options) => ...)`, which registers the context with the repository. Use `r.DatabaseFactory(() => new ApplicationSagaDbContext(options))` when each repository operation should own a fresh context.

**Lifetime change:** EF6's registration-level `DatabaseFactory` registers the base `DbContext` as a scoped service. EF Core's `DatabaseFactory` uses a delegate and creates/releases a context per operation. Use `ExistingDbContext<TContext>` when application work and saga processing need to share a scoped context. Custom `ISagaDbContextFactory<TSaga>` implementations must port synchronous `Release` to `ValueTask ReleaseAsync(DbContext)` and preserve their ownership rules.

## Mapping and concurrency

Port `EntityTypeConfiguration<T>` mappings to EF Core's `EntityTypeBuilder<T>`. Preserve application table/schema names, column names, lengths, nullability, indexes, and relationships explicitly. An inherited default naming convention is not a database migration plan.

Both saga maps disable database generation of `CorrelationId`. EF6 explicitly configures a clustered correlation key. The EF Core SQL Server provider normally makes a primary key clustered; provider-specific customization can be applied through `ConfigureCorrelationIdKey` or the entity map. Other databases have different indexing semantics. [Microsoft's SQL Server index documentation](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/indexes) describes the default and customization options.

Optimistic concurrency needs a real concurrency token. SQL Server's `IsRowVersion()` is a provider-specific mapping; PostgreSQL's `xmin` and application-managed tokens require their corresponding provider mappings. Configure message retry for `DbUpdateConcurrencyException` according to the application's policy. Pessimistic locking and optimistic retry do not deduplicate repeated business messages.

PostgreSQL can raise a serialization failure (`40001`) under `Serializable`, even when pessimistic row locks are used. The entire transaction must be retried; handling only `DbUpdateConcurrencyException` is insufficient. Account for provider exceptions, including wrappers, in the message retry policy. The regression fixture handles serialization and deadlock failures explicitly. See [PostgreSQL transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html).

## Custom loading and lock providers

EF6's `ILoadQueryProvider<TSaga>` is replaced by query delegates for ordinary customization:

```csharp
r.CustomizeQuery(query => query
    .Include(saga => saga.Dependency)
    .ThenInclude(dependency => dependency.Details));
```

For a custom loading algorithm, port the implementation to EF Core's `ILoadQueryExecutor<TSaga>` and repository lock-strategy interfaces. The default factories still accept a custom `ISagaDbContextFactory<TSaga>` and, for pessimistic mode, an `ILockStatementProvider`.

**Custom SQL changes:** EF6 executes a lock-only statement such as `SELECT 1 ... WITH (UPDLOCK, ROWLOCK)` and then loads the entity separately. EF Core uses the lock statement with `FromSqlRaw` to materialize the saga. A custom statement must select the mapped saga columns, use the current table/schema and column names, bind the correlation ID, and apply the provider's locking syntax. Copying an EF6 `SELECT 1` statement is insufficient. The built-in SQL Server and PostgreSQL providers implement the EF Core contract.

The old protected static `SqlLockStatementProvider.TableNames` cache has been removed. Custom subclasses that accessed it must stop depending on a cache shared solely by CLR type. Each provider instance now caches formatted statements by EF model, entity type, and ordered property list; `enableSchemaCaching: false` disables that cache. Pessimistic loaders and the bus outbox delivery service ask the provider for the current context's statement on each operation, so an overridden `GetRowLockStatement` or `GetOutboxStatement` is consulted every time.

Query customization applies to consume loading, not to public detached `Load`/`Find` calls. Pessimistic query correlation selects matching IDs before opening its processing transaction and then loads those IDs under locks; it does not re-evaluate the original predicate after locking. Account for that behavior if application fields used for correlation can change concurrently.

## Audit data

Create the EF Core audit store from configured `DbContextOptions`, a table name, and an optional schema. Use exactly the same table spelling in migration tooling and runtime configuration, particularly for case-sensitive providers.

EF6 maps JSON through backing string properties. EF Core maps `Headers`, `Custom`, and `Message` through value converters. A read audit message can be represented as raw JSON text; use `ObjectDeserializer.Deserialize<TMessage>(record.Message)` to obtain a known message type. Do not rely on the concrete runtime type of the `object` property or on in-place mutation of audit dictionaries for updates. Audit storage is append-oriented.

The default audit model cache includes context type, table, schema, and design-time status. Derived audit contexts that override `OnConfiguring` must call the base implementation. An explicitly supplied `IModelCacheKeyFactory` replacement is preserved; it must account for the audit mapping and any additional dynamic model inputs. It can extend `AuditModelCacheKeyFactory` to reuse the audit key, or include the public `AuditDbContext.AuditTableName` and `AuditTableSchema` values in its own key. This follows [EF Core's model-cache requirements](https://learn.microsoft.com/en-us/ef/core/modeling/dynamic-model). Pooled audit contexts registered through `AddDbContextPool` or `PooledDbContextFactory` keep EF's default model cache, because pooled options cannot be modified; use one derived context type per audit table and schema.

If options use `UseInternalServiceProvider`, the application owns EF service registration. Register `IModelCacheKeyFactory` as a singleton implemented by `AuditModelCacheKeyFactory` in that service collection to support multiple audit tables/schemas. `AuditDbContext` preserves the externally supplied services. Without a suitable custom factory, EF's default cache supports one mapping per context type; it cannot distinguish audit table/schema arguments. The audit store calls `AuditDbContext.EnsureAuditModel()` before each write and throws `InvalidOperationException` when the cached model maps to a different table or schema, instead of writing to the wrong table.

Before using an existing audit table, compare generated identity and column definitions and read representative existing JSON records. Successful fresh-database tests do not demonstrate an in-place schema migration.

## Provider and verification scope

EF6's built-in pessimistic provider is SQL Server. The EF Core regression suite exercises SQL Server, SQL Server with its retrying execution strategy, and PostgreSQL. MySQL, Oracle, and SQLite formatters are present in the source; these tests do not establish their database behavior or migration compatibility. In particular, provider-specific row-lock and transaction semantics require validation with the actual provider and server.

EF Core's inbox/outbox, job saga, and future persistence facilities are additional capabilities. They do not have counterparts in the EF6 integration and require their own configuration and data review.

The retained EF Core suite starts its own SQL Server and PostgreSQL containers through Testcontainers and requires a running Docker engine:

```sh
dotnet test tests/MassTransit.EntityFrameworkCoreIntegration.Tests -c Release --filter 'Category!=Flaky'
```

Tests marked `Flaky` are excluded from this command, and NUnit explicit tests require deliberate selection. Provider-specific integration tests are evidence for the behavior listed here; existing application data still needs an independent schema and rollback check.
