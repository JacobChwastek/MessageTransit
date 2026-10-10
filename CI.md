# Continuous integration

The main workflow builds every tracked C# project on Linux and Windows using the .NET 10 SDK. This includes projects outside `MassTransit.slnx`. Runtime libraries, test processes, and benchmark executables target `net10.0`. The analyzer assembly remains `netstandard2.0` for compiler-host compatibility; its tests run on .NET 10.

## Test matrix

Each name below identifies the matching project directory under `tests/`. The workflow runs Release builds with an explicit `net10.0` test framework. All unattended test runs exclude `Category=Flaky`.

| Project | Execution | Services or providers |
| --- | --- | --- |
| MassTransit.Tests | Linux and Windows | In-memory; includes platform-specific test discovery |
| MassTransit.Abstractions.Tests | Linux and Windows | No external service |
| MassTransit.Analyzers.Tests | Linux and Windows | Roslyn in the .NET 10 test process |
| MassTransit.Interop.NServiceBus.Tests | Linux and Windows | In-memory XML interoperability; project is outside the solution |
| MassTransit.ActiveMqTransport.Tests | Linux | Local ActiveMQ |
| MassTransit.RabbitMqTransport.Tests | Linux | Local RabbitMQ |
| MassTransit.AmazonSqsTransport.Tests | Linux | LocalStack SQS, SNS, and S3 |
| MassTransit.SqlTransport.Tests | Linux | Local SQL Server and PostgreSQL |
| MassTransit.DynamoDbIntegration.Tests | Linux | LocalStack DynamoDB with dummy credentials |
| MassTransit.Azure.Table.Tests | Linux | Local Azurite |
| MassTransit.DapperIntegration.Tests | Linux | Local SQL Server |
| MassTransit.EntityFrameworkCoreIntegration.Tests | Linux | SQL Server and PostgreSQL containers started by the tests through Testcontainers |
| MassTransit.MartenIntegration.Tests | Linux | Local PostgreSQL |
| MassTransit.MongoDbIntegration.Tests | Linux | Local MongoDB replica set |
| MassTransit.NHibernateIntegration.Tests | Linux | In-memory SQLite |
| MassTransit.RedisIntegration.Tests | Two Linux jobs | Local Redis and local Valkey |
| MassTransit.HangfireIntegration.Tests | Linux | In-memory storage |
| MassTransit.QuartzIntegration.Tests | Linux | In-memory scheduler |
| MassTransit.EventHubIntegration.Tests | Linux | Local Event Hubs emulator and Azurite |
| MassTransit.KafkaIntegration.Tests | Linux | Local Kafka, ZooKeeper, and Schema Registry |
| MassTransit.SignalR.Tests | Linux | In-process test fixtures |
| MassTransit.Azure.ServiceBus.Core.Tests | **Not executed** | Requires a real Azure Service Bus namespace and Azure Storage credentials; dedicated cloud test resources are not configured |
| MassTransit.Azure.Cosmos.Tests | **Not executed** | No Cosmos emulator or dedicated cloud test account is provisioned; the RBAC future fixture additionally requires Azure credentials and a remotely configured instance |

The two unavailable cloud suites are still compiled on both operating systems. They are excluded from test execution and are not counted as executed coverage. Azure Table and Event Hubs use development emulators; those results do not establish live Azure compatibility. The EF Core lane does not establish migration or data compatibility with existing EF6 databases.

Tests marked `Integration` are included when their local service is provisioned, including DynamoDB, Dapper, EF Core transaction configuration, and Azure Table saga fixtures.

## Results and exclusions

Every invoked test suite writes a GitHub Actions summary and uploads its TRX results, including on failure. Summaries show the project, framework, operating system, filter, execution counts, and individual skip reasons supplied by the test adapter. A suite producing no result file or executing zero tests fails its reporting step. The workflow also displays this declared matrix independently of the test jobs.

Coverage exclusions apply as follows:

- **Flaky:** tests carrying this category are excluded because their existing timing or environmental behavior is not reliable for unattended CI. Filtered tests do not appear in TRX counts; a green job does not claim they passed.
- **Explicit:** NUnit fixtures and tests requiring deliberate selection remain opt-in. These include stress/performance runs and alternate or external broker scenarios such as AmazonMQ. A default service job does not establish coverage for those scenarios.
- **Ignore:** source-level ignores remain in force. The Cosmos RBAC fixture is one such exclusion; its entire suite is also unavailable in this matrix.
- **Platform:** unsupported-platform tests are reported as skipped. Core tests run on both Windows and Linux. The Windows daylight-saving cron test also returns early when the runner's local timezone has no daylight saving, so a passing Windows job does not prove that branch executed.
- **Benchmarks:** both executables are built on Linux and Windows. CI checks the custom benchmark's `--help` output and BenchmarkDotNet's `--list flat` output. These startup checks do not run workloads, contact brokers, or establish throughput or latency results.

## Manual transport validation

`Transport Validation` is a manually dispatched workflow for RabbitMQ, ActiveMQ, and SQS/S3, using the same .NET 10 test action and local-service configuration as the main workflow. Azure Service Bus is not run because it requires a real namespace and Azure Storage credentials. SQL, Kafka, Event Hubs, and persistence suites are covered by the main workflow. No package publishing job exists in the manual workflow.

Changes to workflow definitions, the shared test action, CI helper scripts, or this matrix trigger the main workflow. Both build lanes include all tracked projects; the three currently outside the solution are the NServiceBus interoperability tests and the two benchmark executables.

## Local commands

From the repository root:

```sh
python3 scripts/ci_build.py
dotnet test tests/MassTransit.Analyzers.Tests/MassTransit.Analyzers.Tests.csproj -c Release -f net10.0 --filter 'Category!=Flaky'
dotnet test tests/MassTransit.Interop.NServiceBus.Tests/MassTransit.Interop.NServiceBus.Tests.csproj -c Release -f net10.0 --filter 'Category!=Flaky'
```

Service-dependent suites require the corresponding local service before execution. The workflow files contain their service configuration. CI configuration describes intended coverage; only a completed run and its retained results establish which checks actually passed.
