# Project and dependency inventory

This snapshot records the retained projects and their declared build, package and test relationships. It shows how the solution, packages and tests are connected. It does not establish that a project builds, its tests pass, or its packages can be published.

The [structured inventory](dependency-inventory.json) contains every tracked project, effective package metadata, target-specific direct dependencies, central versions, original conditions, solution membership and reverse project references. The [generator](../../scripts/dependency_inventory.py) refreshes it from repository inputs and MSBuild evaluation without restoring or building.

## Scope and consumers

- All **59** tracked projects are retained: **33 source projects**, **24 test projects** and **2 benchmarks**. The solution contains **56** projects.
- The projects outside the solution are `MassTransit.Benchmark`, `MassTransit.BenchmarkConsole`, and `MassTransit.Interop.NServiceBus.Tests`, all under `tests/`. They require separate build/test commands; solution commands do not include them automatically.
- Repository consumers are identified through project, test and benchmark references. Application deployments and persisted data are outside the scope of this inventory.
- A project reference from a test project records reachability, not behavioral coverage. Benchmarks inherit `IsTestProject=true` from `tests/Directory.Build.props`; they remain classified as benchmarks here.

## Source projects

All 33 source projects evaluate as packable. **Direct tests** counts test-project references, excluding benchmarks; exact test and benchmark names are in each JSON record. Every runtime source project targets `net10.0` on all platforms. The analyzer retains `netstandard2.0` for compiler-host compatibility and ships under `analyzers/dotnet/cs`; it does not extend runtime support. Windows columns describe property evaluation on the local host, not a Windows build.

| Source project | Effective NuGet ID | Non-Windows targets | Windows adds | Direct tests |
| --- | --- | --- | --- | --- |
| [MassTransit.Abstractions](../../src/MassTransit.Abstractions/MassTransit.Abstractions.csproj) | `MassTransit.Abstractions` | `net10.0` | — | 1 |
| [MassTransit.Analyzers](../../src/MassTransit.Analyzers/MassTransit.Analyzers.csproj) | `MassTransit.Analyzers` | `netstandard2.0` | — | 1 |
| [MassTransit.Interop.NServiceBus](../../src/MassTransit.Interop.NServiceBus/MassTransit.Interop.NServiceBus.csproj) | `MassTransit.Interop.NServiceBus` | `net10.0` | — | 2 |
| [MassTransit.MessagePack](../../src/MassTransit.MessagePack/MassTransit.MessagePack.csproj) | `MassTransit.MessagePack` | `net10.0` | — | 1 |
| [MassTransit.Newtonsoft](../../src/MassTransit.Newtonsoft/MassTransit.Newtonsoft.csproj) | `MassTransit.Newtonsoft` | `net10.0` | — | 6 |
| [MassTransit.SignalR](../../src/MassTransit.SignalR/MassTransit.SignalR.csproj) | `MassTransit.SignalR` | `net10.0` | — | 1 |
| [MassTransit.StateMachineVisualizer](../../src/MassTransit.StateMachineVisualizer/MassTransit.StateMachineVisualizer.csproj) | `MassTransit.StateMachineVisualizer` | `net10.0` | — | 1 |
| [MassTransit.TestFramework](../../src/MassTransit.TestFramework/MassTransit.TestFramework.csproj) | `MassTransit.TestFramework` | `net10.0` | — | 20 |
| [MassTransit](../../src/MassTransit/MassTransit.csproj) | `MassTransit` | `net10.0` | — | 19 |
| [MassTransit.AmazonS3](../../src/Persistence/MassTransit.AmazonS3/MassTransit.AmazonS3.csproj) | `MassTransit.AmazonS3` | `net10.0` | — | 1 |
| [MassTransit.Azure.Cosmos](../../src/Persistence/MassTransit.Azure.Cosmos/MassTransit.Azure.Cosmos.csproj) | `MassTransit.Azure.Cosmos` | `net10.0` | — | 1 |
| [MassTransit.Azure.Storage](../../src/Persistence/MassTransit.Azure.Storage/MassTransit.Azure.Storage.csproj) | `MassTransit.Azure.Storage` | `net10.0` | — | 1 |
| [MassTransit.Azure.Table](../../src/Persistence/MassTransit.Azure.Table/MassTransit.Azure.Table.csproj) | `MassTransit.Azure.Cosmos.Table` | `net10.0` | — | 1 |
| [MassTransit.DapperIntegration](../../src/Persistence/MassTransit.DapperIntegration/MassTransit.DapperIntegration.csproj) | `MassTransit.DapperIntegration` | `net10.0` | — | 1 |
| [MassTransit.DynamoDbIntegration](../../src/Persistence/MassTransit.DynamoDbIntegration/MassTransit.DynamoDbIntegration.csproj) | `MassTransit.DynamoDb` | `net10.0` | — | 1 |
| [MassTransit.EntityFrameworkCoreIntegration](../../src/Persistence/MassTransit.EntityFrameworkCoreIntegration/MassTransit.EntityFrameworkCoreIntegration.csproj) | `MassTransit.EntityFrameworkCore` | `net10.0` | — | 2 |
| [MassTransit.EntityFrameworkIntegration](../../src/Persistence/MassTransit.EntityFrameworkIntegration/MassTransit.EntityFrameworkIntegration.csproj) | `MassTransit.EntityFramework` | `net10.0` | — | 1 |
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

The following is the declared configuration in [build.yml](../../.github/workflows/build.yml), inspected on 2026-10-03. Its 23 jobs are mapped below. Every test invocation excludes `Category=Flaky`; rows marked **I** also exclude `Category=Integration`. A listed service is configured by the workflow; availability and passing runs were not checked.

| Job | Projects or responsibility | Requirements and current limits |
| --- | --- | --- |
| `compile` | Solution restore and Release build | Ubuntu and Windows; installs SDK 10.0.x; excludes the three projects outside the solution. |
| `test-ubuntu` | `MassTransit.Tests`, `MassTransit.Abstractions.Tests` | Explicit `-f net10.0`. |
| `test-activemq` | `MassTransit.ActiveMqTransport.Tests` | ActiveMQ service. |
| `test-sql-transport` | `MassTransit.SqlTransport.Tests` | SQL Server and PostgreSQL services; **I**. |
| `test-azure-service-bus` | `MassTransit.Azure.ServiceBus.Core.Tests` | Disabled by `if: false`; Azure service bus/storage configuration. |
| `test-rabbitmq` | `MassTransit.RabbitMqTransport.Tests` | RabbitMQ service. |
| `test-sqs` | `MassTransit.AmazonSqsTransport.Tests` | LocalStack service; exercises S3 as well as SQS. |
| `test-azure-table` | `MassTransit.Azure.Table.Tests` | Azurite via project Compose file; Azure storage configuration; **I**. |
| `test-cosmosdb` | `MassTransit.Azure.Cosmos.Tests` | Restricted to upstream `MassTransit/MassTransit` on master/develop; not enabled for this fork; **I**. |
| `test-dapper` | `MassTransit.DapperIntegration.Tests` | SQL Server service; **I**. |
| `test-entity-framework` | EF Core and EF6 integration test projects | SQL Server and PostgreSQL services; **I**. |
| `test-marten` | `MassTransit.MartenIntegration.Tests` | PostgreSQL service. |
| `test-mongo` | `MassTransit.MongoDbIntegration.Tests` | MongoDB via project Compose file. |
| `test-nhibernate` | `MassTransit.NHibernateIntegration.Tests` | No database service provisioned by this job; test/provider configuration still needs execution validation. |
| `test-redis` | `MassTransit.RedisIntegration.Tests` | Redis service. |
| `test-valkey` | `MassTransit.RedisIntegration.Tests` | Valkey service. |
| `test-hangfire` | `MassTransit.HangfireIntegration.Tests` | No external service declared by the job. |
| `test-quartz` | `MassTransit.QuartzIntegration.Tests` | No external service declared by the job. |
| `test-eventhub` | `MassTransit.EventHubIntegration.Tests` | Event Hubs emulator and Azurite via Compose; Azure configuration; omitted from `calc-version.needs`. |
| `test-kafka` | `MassTransit.KafkaIntegration.Tests` | Kafka, ZooKeeper and Schema Registry via Compose. |
| `test-signalr` | `MassTransit.SignalR.Tests` | No external service declared by the job. |
| `calc-version` | Derive stable/develop version from fixed `MASSTRANSIT_VERSION` | Depends on build and listed tests, including the upstream-only Cosmos job; release-chain behavior needs repair/validation. |
| `publish` | Solution build/pack and NuGet push | Windows; upstream-repository/master/develop condition prevents fork publication. All package identities are still inherited. |

The separate [nightly-transports.yml](../../.github/workflows/nightly-transports.yml) contains one `test-transports` job. Despite its filename, it is **manual only** (`workflow_dispatch`). It installs SDKs 3.1 and 5.0 and runs in the absent `tests/MassTransit.Transports.Tests` directory. Its broker services do not make it usable with the current project layout.

`MassTransit.Analyzers.Tests`, `MassTransit.DynamoDbIntegration.Tests`, and `MassTransit.Interop.NServiceBus.Tests` have no explicit test invocation in either workflow. The first two are solution build inputs; the interop test project and both benchmarks are outside the solution. The two WebJobs source integrations have no direct test-project reference in this repository. Other source projects can be referenced by shared tests without having their own named test project; see the per-project mapping above and the JSON.

The project files now target .NET 10, but the obsolete manual workflow and release gates still require updates to match the retained project layout. No workflow runs, broker services, cloud accounts, package feeds or database contents were queried or changed by this inventory.

## EF6 and persisted state

The [EF6 integration](../../src/Persistence/MassTransit.EntityFrameworkIntegration/MassTransit.EntityFrameworkIntegration.csproj) publishes as `MassTransit.EntityFramework`, depends on `EntityFramework` **6.5.2**, and now targets `net10.0` during the EF Core transition. Its only direct in-repository consumer is [MassTransit.EntityFrameworkIntegration.Tests](../../tests/MassTransit.EntityFrameworkIntegration.Tests/MassTransit.EntityFrameworkIntegration.Tests.csproj), also targeting `net10.0`. No other source project references it.

The retained EF6 functionality includes saga persistence with optimistic/pessimistic concurrency and an [audit store](../../src/Persistence/MassTransit.EntityFrameworkIntegration/EntityFrameworkIntegration/Audit/EntityFrameworkAuditStore.cs). Test contexts store saga and audit rows in a local SQL Server/LocalDB test database; the [audit fixture](../../tests/MassTransit.EntityFrameworkIntegration.Tests/AuditStore_Specs.cs) uses `DropCreateDatabaseAlways`. These are test fixtures, not evidence of deployed user data. EF6 tests call `UseInMemoryOutbox`; the EF Core [reliable-messaging context](../../tests/MassTransit.EntityFrameworkCoreIntegration.Tests/ReliableMessaging/ReliableDbContext.cs) separately declares transactional outbox entities. Do not treat those as the same storage model.

An EF6-to-EF Core migration requires comparing saga concurrency, audit behavior and database schemas. Repository test fixtures do not establish a migration path for application databases.

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
