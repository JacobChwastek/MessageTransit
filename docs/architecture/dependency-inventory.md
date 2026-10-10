# Project and dependency inventory

This snapshot records the retained projects and their declared build, package and test relationships. It shows how the solution, packages and tests are connected. It does not establish that a project builds, its tests pass, or its packages can be published.

The [structured inventory](dependency-inventory.json) contains every tracked project, effective package metadata, target-specific direct dependencies, central versions, original conditions, solution membership and reverse project references. The [generator](../../scripts/dependency_inventory.py) refreshes it from repository inputs and MSBuild evaluation without restoring or building.

## Scope and consumers

- The retained set contains **58** tracked projects: **32 source projects**, **23 test projects**, **2 benchmarks** and **1 test-support library** (`MassTransit.Testing.Containers`, the shared Testcontainers setup). The solution contains **55** projects.
- The projects outside the solution are `MassTransit.Benchmark`, `MassTransit.BenchmarkConsole`, and `MassTransit.Interop.NServiceBus.Tests`, all under `tests/`. They require separate build/test commands; solution commands do not include them automatically.
- Repository consumers are identified through project, test and benchmark references. Application deployments and persisted data are outside the scope of this inventory.
- A project reference from a test project records reachability, not behavioral coverage. Benchmarks inherit `IsTestProject=true` from `tests/Directory.Build.props`; they remain classified as benchmarks here. Test projects end in `.Tests` or, when they start their own containers, `.IntegrationTests`.

## Source projects

All 32 source projects evaluate as packable. **Direct tests** counts test-project references, excluding benchmarks; exact test and benchmark names are in each JSON record. Every runtime source project targets `net10.0` on all platforms. The analyzer retains `netstandard2.0` for compiler-host compatibility and ships under `analyzers/dotnet/cs`; it does not extend runtime support. Windows columns describe property evaluation on the local host, not a Windows build.

| Source project | Effective NuGet ID | Non-Windows targets | Windows adds | Direct tests |
| --- | --- | --- | --- | --- |
| [MassTransit.Abstractions](../../src/MassTransit.Abstractions/MassTransit.Abstractions.csproj) | `MassTransit.Abstractions` | `net10.0` | — | 1 |
| [MassTransit.Analyzers](../../src/MassTransit.Analyzers/MassTransit.Analyzers.csproj) | `MassTransit.Analyzers` | `netstandard2.0` | — | 1 |
| [MassTransit.Interop.NServiceBus](../../src/MassTransit.Interop.NServiceBus/MassTransit.Interop.NServiceBus.csproj) | `MassTransit.Interop.NServiceBus` | `net10.0` | — | 2 |
| [MassTransit.MessagePack](../../src/MassTransit.MessagePack/MassTransit.MessagePack.csproj) | `MassTransit.MessagePack` | `net10.0` | — | 1 |
| [MassTransit.Newtonsoft](../../src/MassTransit.Newtonsoft/MassTransit.Newtonsoft.csproj) | `MassTransit.Newtonsoft` | `net10.0` | — | 6 |
| [MassTransit.SignalR](../../src/MassTransit.SignalR/MassTransit.SignalR.csproj) | `MassTransit.SignalR` | `net10.0` | — | 1 |
| [MassTransit.StateMachineVisualizer](../../src/MassTransit.StateMachineVisualizer/MassTransit.StateMachineVisualizer.csproj) | `MassTransit.StateMachineVisualizer` | `net10.0` | — | 1 |
| [MassTransit.TestFramework](../../src/MassTransit.TestFramework/MassTransit.TestFramework.csproj) | `MassTransit.TestFramework` | `net10.0` | — | 19 |
| [MassTransit](../../src/MassTransit/MassTransit.csproj) | `MassTransit` | `net10.0` | — | 18 |
| [MassTransit.AmazonS3](../../src/Persistence/MassTransit.AmazonS3/MassTransit.AmazonS3.csproj) | `MassTransit.AmazonS3` | `net10.0` | — | 1 |
| [MassTransit.Azure.Cosmos](../../src/Persistence/MassTransit.Azure.Cosmos/MassTransit.Azure.Cosmos.csproj) | `MassTransit.Azure.Cosmos` | `net10.0` | — | 1 |
| [MassTransit.Azure.Storage](../../src/Persistence/MassTransit.Azure.Storage/MassTransit.Azure.Storage.csproj) | `MassTransit.Azure.Storage` | `net10.0` | — | 1 |
| [MassTransit.Azure.Table](../../src/Persistence/MassTransit.Azure.Table/MassTransit.Azure.Table.csproj) | `MassTransit.Azure.Cosmos.Table` | `net10.0` | — | 1 |
| [MassTransit.DapperIntegration](../../src/Persistence/MassTransit.DapperIntegration/MassTransit.DapperIntegration.csproj) | `MassTransit.DapperIntegration` | `net10.0` | — | 1 |
| [MassTransit.DynamoDbIntegration](../../src/Persistence/MassTransit.DynamoDbIntegration/MassTransit.DynamoDbIntegration.csproj) | `MassTransit.DynamoDb` | `net10.0` | — | 1 |
| [MassTransit.EntityFrameworkCoreIntegration](../../src/Persistence/MassTransit.EntityFrameworkCoreIntegration/MassTransit.EntityFrameworkCoreIntegration.csproj) | `MassTransit.EntityFrameworkCore` | `net10.0` | — | 2 |
| [MassTransit.MartenIntegration](../../src/Persistence/MassTransit.MartenIntegration/MassTransit.MartenIntegration.csproj) | `MassTransit.Marten` | `net10.0` | — | 1 |
| [MassTransit.MongoDbIntegration](../../src/Persistence/MassTransit.MongoDbIntegration/MassTransit.MongoDbIntegration.csproj) | `MassTransit.MongoDb` | `net10.0` | — | 1 |
| [MassTransit.NHibernateIntegration](../../src/Persistence/MassTransit.NHibernateIntegration/MassTransit.NHibernateIntegration.csproj) | `MassTransit.NHibernate` | `net10.0` | — | 1 |
| [MassTransit.RedisIntegration](../../src/Persistence/MassTransit.RedisIntegration/MassTransit.RedisIntegration.csproj) | `MassTransit.Redis` | `net10.0` | — | 1 |
| [MassTransit.HangfireIntegration](../../src/Scheduling/MassTransit.HangfireIntegration/MassTransit.HangfireIntegration.csproj) | `MassTransit.Hangfire` | `net10.0` | — | 1 |
| [MassTransit.QuartzIntegration](../../src/Scheduling/MassTransit.QuartzIntegration/MassTransit.QuartzIntegration.csproj) | `MassTransit.Quartz` | `net10.0` | — | 6 |
| [MassTransit.ActiveMqTransport](../../src/Transports/MassTransit.ActiveMqTransport/MassTransit.ActiveMqTransport.csproj) | `MassTransit.ActiveMQ` | `net10.0` | — | 1 |
| [MassTransit.AmazonSqsTransport](../../src/Transports/MassTransit.AmazonSqsTransport/MassTransit.AmazonSqsTransport.csproj) | `MassTransit.AmazonSQS` | `net10.0` | — | 1 |
| [MassTransit.Azure.ServiceBus.Core](../../src/Transports/MassTransit.Azure.ServiceBus.Core/MassTransit.Azure.ServiceBus.Core.csproj) | `MassTransit.Azure.ServiceBus.Core` | `net10.0` | — | 1 |
| [MassTransit.EventHubIntegration](../../src/Transports/MassTransit.EventHubIntegration/MassTransit.EventHubIntegration.csproj) | `MassTransit.EventHub` | `net10.0` | — | 1 |
| [MassTransit.KafkaIntegration](../../src/Transports/MassTransit.KafkaIntegration/MassTransit.KafkaIntegration.csproj) | `MassTransit.Kafka` | `net10.0` | — | 1 |
| [MassTransit.RabbitMqTransport](../../src/Transports/MassTransit.RabbitMqTransport/MassTransit.RabbitMqTransport.csproj) | `MassTransit.RabbitMQ` | `net10.0` | — | 1 |
| [MassTransit.SqlTransport.PostgreSql](../../src/Transports/MassTransit.SqlTransport.PostgreSql/MassTransit.SqlTransport.PostgreSql.csproj) | `MassTransit.SqlTransport.PostgreSQL` | `net10.0` | — | 1 |
| [MassTransit.SqlTransport.SqlServer](../../src/Transports/MassTransit.SqlTransport.SqlServer/MassTransit.SqlTransport.SqlServer.csproj) | `MassTransit.SqlTransport.SqlServer` | `net10.0` | — | 1 |
| [MassTransit.WebJobs.EventHubsIntegration](../../src/Transports/MassTransit.WebJobs.EventHubsIntegration/MassTransit.WebJobs.EventHubsIntegration.csproj) | `MassTransit.WebJobs.EventHubs` | `net10.0` | — | 0 |
| [MassTransit.WebJobs.ServiceBusIntegration](../../src/Transports/MassTransit.WebJobs.ServiceBusIntegration/MassTransit.WebJobs.ServiceBusIntegration.csproj) | `MassTransit.WebJobs.ServiceBus` | `net10.0` | — | 0 |

For every source project, build status is **evaluated, not built in this inventory**, package status is **declared/evaluated, not publication-validated**, and consumer status covers **repository references; external application usage is not assessed**. Exact direct and reverse dependencies, test-project references, framework references and conditional versions are in the JSON. All source projects are in the solution; test/benchmark exclusions are listed above.

## CI jobs

The [main workflow](../../.github/workflows/build.yml) defines the build, test, and package jobs. See the maintained [CI matrix](../../CI.md) for execution platforms, service requirements, excluded tests, and unavailable cloud suites. Both build lanes include projects outside the solution. The `test-entity-framework` job runs the retained EF Core suite, which provisions SQL Server and PostgreSQL through Testcontainers.

The separate [transport validation workflow](../../.github/workflows/nightly-transports.yml) is manually dispatched and covers RabbitMQ, ActiveMQ, and SQS/S3. It does not publish packages.

Workflow configuration records intended coverage. This inventory does not establish successful builds, executed tests, service availability, or package publication. The two WebJobs source integrations have no direct test-project reference; other source projects can also be referenced by shared tests without having their own named test project.

## Entity Framework persistence

The EF6 adapter and its test project have been removed. No `MessageTransit.EntityFramework` package is planned. The retained [EF Core integration](../../src/Persistence/MassTransit.EntityFrameworkCoreIntegration/MassTransit.EntityFrameworkCoreIntegration.csproj) provides saga and audit persistence and currently builds as `MassTransit.EntityFrameworkCore`.

The [migration guide](../../ENTITY_FRAMEWORK_MIGRATION.md) maps EF6 behavior to retained EF Core regression coverage and explains context, mapping, locking, and audit differences. Existing application databases require their own schema, data, and rollback validation; fresh test databases do not establish an in-place migration contract.

EF Core also provides transactional inbox/outbox storage, as shown by the [reliable-messaging context](../../tests/MassTransit.EntityFrameworkCoreIntegration.IntegrationTests/ReliableMessaging/ReliableDbContext.cs). Those tables are separate from saga/audit storage and had no counterpart in the removed EF6 adapter.

## Refresh and verification

From the repository root, with Python 3.9+, Git and a .NET SDK supporting MSBuild property/item queries:

```sh
python3 scripts/dependency_inventory.py
python3 scripts/dependency_inventory.py --check
```

The saved evaluation uses **SDK 10.0.400 / MSBuild 18.9.6**, Release configuration and Windows/non-Windows property variants. Current source conditions remain in `source_files`; `input_sha256` detects changes to tracked project, props, targets, solution and workflow inputs. Configuration-specific dependency behavior requires additional evaluation contexts. SDK changes can legitimately change evaluated implicit metadata and require a regenerated snapshot.

`--check` re-evaluates without writing and fails for stale output, evaluation failures or unresolved package versions. New projects must be tracked before they become part of this tracked-project inventory. Each source record explicitly separates build, package, dependency, test and consumer status.

The generator records repository declarations and evaluated direct dependencies. It does not resolve the full transitive NuGet graph, prove dependency/target compatibility, execute tests, inspect live data or validate package archives. Those remain explicit validation work. Existing `obj/project.assets.json` files are not treated as current restore evidence.

See the [redistribution notes](../legal/redistribution-inventory.md) for license and attribution requirements and unresolved redistribution questions.

The repository SDK selector in [global.json](../../global.json) allows stable .NET 10 feature bands. The inventory hashes that selector along with its other build inputs.
