# Project and dependency inventory

This snapshot records the retained projects and their declared build, package and test relationships. It shows how the solution, packages and tests are connected. It does not establish that a project builds, its tests pass, or its packages can be published.

The [structured inventory](dependency-inventory.json) contains every tracked project, effective package metadata, target-specific direct dependencies, central versions, original conditions, solution membership and reverse project references. The [generator](../../scripts/dependency_inventory.py) refreshes it from repository inputs and MSBuild evaluation without restoring or building.

## Scope and consumers

- The retained set contains **58** tracked projects: **32 source projects**, **23 test projects**, **2 benchmarks** and **1 test-support library** (`MessageTransit.Testing.Containers`, the shared Testcontainers setup). The solution contains **55** projects.
- The projects outside the solution are `MessageTransit.Benchmark`, `MessageTransit.BenchmarkConsole`, and `MessageTransit.Interop.NServiceBus.Tests`, all under `tests/`. They require separate build/test commands; solution commands do not include them automatically.
- Repository consumers are identified through project, test and benchmark references. Application deployments and persisted data are outside the scope of this inventory.
- A project reference from a test project records reachability, not behavioral coverage. Benchmarks inherit `IsTestProject=true` from `tests/Directory.Build.props`; they remain classified as benchmarks here. Test projects end in `.Tests` or, when they start their own containers, `.IntegrationTests`.

## Source projects

All 32 source projects evaluate as packable. **Direct tests** counts test-project references, excluding benchmarks; exact test and benchmark names are in each JSON record. Every runtime source project targets `net10.0` on all platforms. The analyzer retains `netstandard2.0` for compiler-host compatibility and ships under `analyzers/dotnet/cs`; it does not extend runtime support. Windows columns describe property evaluation on the local host, not a Windows build.

| Source project | Effective NuGet ID | Non-Windows targets | Windows adds | Direct tests |
| --- | --- | --- | --- | --- |
| [MessageTransit.Abstractions](../../src/MessageTransit.Abstractions/MessageTransit.Abstractions.csproj) | `MessageTransit.Abstractions` | `net10.0` | — | 1 |
| [MessageTransit.Analyzers](../../src/MessageTransit.Analyzers/MessageTransit.Analyzers.csproj) | `MessageTransit.Analyzers` | `netstandard2.0` | — | 1 |
| [MessageTransit.Interop.NServiceBus](../../src/MessageTransit.Interop.NServiceBus/MessageTransit.Interop.NServiceBus.csproj) | `MessageTransit.Interop.NServiceBus` | `net10.0` | — | 2 |
| [MessageTransit.MessagePack](../../src/MessageTransit.MessagePack/MessageTransit.MessagePack.csproj) | `MessageTransit.MessagePack` | `net10.0` | — | 1 |
| [MessageTransit.Newtonsoft](../../src/MessageTransit.Newtonsoft/MessageTransit.Newtonsoft.csproj) | `MessageTransit.Newtonsoft` | `net10.0` | — | 6 |
| [MessageTransit.SignalR](../../src/MessageTransit.SignalR/MessageTransit.SignalR.csproj) | `MessageTransit.SignalR` | `net10.0` | — | 1 |
| [MessageTransit.StateMachineVisualizer](../../src/MessageTransit.StateMachineVisualizer/MessageTransit.StateMachineVisualizer.csproj) | `MessageTransit.StateMachineVisualizer` | `net10.0` | — | 1 |
| [MessageTransit.TestFramework](../../src/MessageTransit.TestFramework/MessageTransit.TestFramework.csproj) | `MessageTransit.TestFramework` | `net10.0` | — | 19 |
| [MessageTransit](../../src/MessageTransit/MessageTransit.csproj) | `MessageTransit` | `net10.0` | — | 18 |
| [MessageTransit.AmazonS3](../../src/Persistence/MessageTransit.AmazonS3/MessageTransit.AmazonS3.csproj) | `MessageTransit.AmazonS3` | `net10.0` | — | 1 |
| [MessageTransit.Azure.Cosmos](../../src/Persistence/MessageTransit.Azure.Cosmos/MessageTransit.Azure.Cosmos.csproj) | `MessageTransit.Azure.Cosmos` | `net10.0` | — | 1 |
| [MessageTransit.Azure.Storage](../../src/Persistence/MessageTransit.Azure.Storage/MessageTransit.Azure.Storage.csproj) | `MessageTransit.Azure.Storage` | `net10.0` | — | 1 |
| [MessageTransit.Azure.Table](../../src/Persistence/MessageTransit.Azure.Table/MessageTransit.Azure.Table.csproj) | `MessageTransit.Azure.Cosmos.Table` | `net10.0` | — | 1 |
| [MessageTransit.DapperIntegration](../../src/Persistence/MessageTransit.DapperIntegration/MessageTransit.DapperIntegration.csproj) | `MessageTransit.DapperIntegration` | `net10.0` | — | 1 |
| [MessageTransit.DynamoDbIntegration](../../src/Persistence/MessageTransit.DynamoDbIntegration/MessageTransit.DynamoDbIntegration.csproj) | `MessageTransit.DynamoDb` | `net10.0` | — | 1 |
| [MessageTransit.EntityFrameworkCoreIntegration](../../src/Persistence/MessageTransit.EntityFrameworkCoreIntegration/MessageTransit.EntityFrameworkCoreIntegration.csproj) | `MessageTransit.EntityFrameworkCore` | `net10.0` | — | 2 |
| [MessageTransit.MartenIntegration](../../src/Persistence/MessageTransit.MartenIntegration/MessageTransit.MartenIntegration.csproj) | `MessageTransit.Marten` | `net10.0` | — | 1 |
| [MessageTransit.MongoDbIntegration](../../src/Persistence/MessageTransit.MongoDbIntegration/MessageTransit.MongoDbIntegration.csproj) | `MessageTransit.MongoDb` | `net10.0` | — | 1 |
| [MessageTransit.NHibernateIntegration](../../src/Persistence/MessageTransit.NHibernateIntegration/MessageTransit.NHibernateIntegration.csproj) | `MessageTransit.NHibernate` | `net10.0` | — | 1 |
| [MessageTransit.RedisIntegration](../../src/Persistence/MessageTransit.RedisIntegration/MessageTransit.RedisIntegration.csproj) | `MessageTransit.Redis` | `net10.0` | — | 1 |
| [MessageTransit.HangfireIntegration](../../src/Scheduling/MessageTransit.HangfireIntegration/MessageTransit.HangfireIntegration.csproj) | `MessageTransit.Hangfire` | `net10.0` | — | 1 |
| [MessageTransit.QuartzIntegration](../../src/Scheduling/MessageTransit.QuartzIntegration/MessageTransit.QuartzIntegration.csproj) | `MessageTransit.Quartz` | `net10.0` | — | 6 |
| [MessageTransit.ActiveMqTransport](../../src/Transports/MessageTransit.ActiveMqTransport/MessageTransit.ActiveMqTransport.csproj) | `MessageTransit.ActiveMQ` | `net10.0` | — | 1 |
| [MessageTransit.AmazonSqsTransport](../../src/Transports/MessageTransit.AmazonSqsTransport/MessageTransit.AmazonSqsTransport.csproj) | `MessageTransit.AmazonSQS` | `net10.0` | — | 1 |
| [MessageTransit.Azure.ServiceBus.Core](../../src/Transports/MessageTransit.Azure.ServiceBus.Core/MessageTransit.Azure.ServiceBus.Core.csproj) | `MessageTransit.Azure.ServiceBus.Core` | `net10.0` | — | 1 |
| [MessageTransit.EventHubIntegration](../../src/Transports/MessageTransit.EventHubIntegration/MessageTransit.EventHubIntegration.csproj) | `MessageTransit.EventHub` | `net10.0` | — | 1 |
| [MessageTransit.KafkaIntegration](../../src/Transports/MessageTransit.KafkaIntegration/MessageTransit.KafkaIntegration.csproj) | `MessageTransit.Kafka` | `net10.0` | — | 1 |
| [MessageTransit.RabbitMqTransport](../../src/Transports/MessageTransit.RabbitMqTransport/MessageTransit.RabbitMqTransport.csproj) | `MessageTransit.RabbitMQ` | `net10.0` | — | 1 |
| [MessageTransit.SqlTransport.PostgreSql](../../src/Transports/MessageTransit.SqlTransport.PostgreSql/MessageTransit.SqlTransport.PostgreSql.csproj) | `MessageTransit.SqlTransport.PostgreSQL` | `net10.0` | — | 1 |
| [MessageTransit.SqlTransport.SqlServer](../../src/Transports/MessageTransit.SqlTransport.SqlServer/MessageTransit.SqlTransport.SqlServer.csproj) | `MessageTransit.SqlTransport.SqlServer` | `net10.0` | — | 1 |
| [MessageTransit.WebJobs.EventHubsIntegration](../../src/Transports/MessageTransit.WebJobs.EventHubsIntegration/MessageTransit.WebJobs.EventHubsIntegration.csproj) | `MessageTransit.WebJobs.EventHubs` | `net10.0` | — | 0 |
| [MessageTransit.WebJobs.ServiceBusIntegration](../../src/Transports/MessageTransit.WebJobs.ServiceBusIntegration/MessageTransit.WebJobs.ServiceBusIntegration.csproj) | `MessageTransit.WebJobs.ServiceBus` | `net10.0` | — | 0 |

For every source project, build status is **evaluated, not built in this inventory**, package status is **declared/evaluated, not publication-validated**, and consumer status covers **repository references; external application usage is not assessed**. Exact direct and reverse dependencies, test-project references, framework references and conditional versions are in the JSON. All source projects are in the solution; test/benchmark exclusions are listed above.

## CI jobs

The [main workflow](../../.github/workflows/build.yml) defines the build, the `*.Tests` jobs, and the package jobs. The [integration workflow](../../.github/workflows/integration.yml) runs the `*.IntegrationTests` suites, which provision their own containers through Testcontainers, on pull requests into `master` and nightly. See the maintained [CI matrix](../../CI.md) for execution platforms, service requirements, excluded tests, and unavailable cloud suites. Both build lanes include projects outside the solution.

The separate [transport validation workflow](../../.github/workflows/nightly-transports.yml) is manually dispatched and covers RabbitMQ, ActiveMQ, and SQS/S3. It does not publish packages.

Workflow configuration records intended coverage. This inventory does not establish successful builds, executed tests, service availability, or package publication. The two WebJobs source integrations have no direct test-project reference; other source projects can also be referenced by shared tests without having their own named test project.

## Entity Framework persistence

The EF6 adapter and its test project have been removed. No `MessageTransit.EntityFramework` package is planned. The retained [EF Core integration](../../src/Persistence/MessageTransit.EntityFrameworkCoreIntegration/MessageTransit.EntityFrameworkCoreIntegration.csproj) provides saga and audit persistence and currently builds as `MessageTransit.EntityFrameworkCore`.

The [migration guide](../../ENTITY_FRAMEWORK_MIGRATION.md) maps EF6 behavior to retained EF Core regression coverage and explains context, mapping, locking, and audit differences. Existing application databases require their own schema, data, and rollback validation; fresh test databases do not establish an in-place migration contract.

EF Core also provides transactional inbox/outbox storage, as shown by the [reliable-messaging context](../../tests/MessageTransit.EntityFrameworkCoreIntegration.IntegrationTests/ReliableMessaging/ReliableDbContext.cs). Those tables are separate from saga/audit storage and had no counterpart in the removed EF6 adapter.

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
