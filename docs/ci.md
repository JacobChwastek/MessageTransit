# Continuous integration

The main workflow builds every tracked C# project on Linux and Windows using the .NET 10 SDK. This includes projects outside `MessageTransit.slnx`. Runtime libraries, test processes, and benchmark executables target `net10.0`. The analyzer assembly remains `netstandard2.0` for compiler-host compatibility; its tests run on .NET 10.

## When tests run

The build and integration workflows divide the source suites; the release workflow requires both:

| Workflow | Runs | Triggers |
| --- | --- | --- |
| [build.yml](../.github/workflows/build.yml) | Build on Linux and Windows, `*.Tests` projects, and the suites that still use workflow service containers (Dapper, Marten, DynamoDB) | Every push and every pull request touching build inputs, and each release |
| [integration.yml](../.github/workflows/integration.yml) | Every `tests/*.IntegrationTests` project except Cosmos DB, plus the Redis suite against Valkey, one matrix leg each | Pull requests into `master`, nightly on the default branch, and each release |
| [release.yml](../.github/workflows/release.yml) | The full build and integration workflows, inspected packages, and isolated package consumers; publishing waits for approval | Every push to `master`; a manual run is a dry run unless it publishes from `master` |

`integration.yml` discovers its suites from the `*.IntegrationTests` directories, so a new container-based suite needs no workflow change. Its `Integration` job passes only when every leg passes and is the check to require on `master`. Ordinary branch and pull request runs cancel superseded runs. Release calls use their own concurrency groups so unrelated validation cannot cancel the candidate's checks.

## Test matrix

Each name below identifies the matching project directory under `tests/`. The workflows run Release builds with an explicit `net10.0` test framework. All unattended test runs exclude `Category=Flaky`.

| Project | Execution | Services or providers |
| --- | --- | --- |
| MessageTransit.Tests | Linux and Windows | In-memory; includes platform-specific test discovery |
| MessageTransit.Abstractions.Tests | Linux and Windows | No external service |
| MessageTransit.Analyzers.Tests | Linux and Windows | Roslyn in the .NET 10 test process |
| MessageTransit.Interop.NServiceBus.Tests | Linux and Windows | In-memory XML interoperability; project is outside the solution |
| MessageTransit.ActiveMqTransport.IntegrationTests | Linux | ActiveMQ container started by the tests through Testcontainers; fixed host ports (Artemis starts only for the excluded Artemis cases) |
| MessageTransit.RabbitMqTransport.IntegrationTests | Linux | RabbitMQ container started by the tests through Testcontainers; fixed host ports |
| MessageTransit.AmazonSqsTransport.IntegrationTests | Linux | LocalStack (SQS, SNS, and S3) container started by the tests through Testcontainers; fixed host ports |
| MessageTransit.SqlTransport.IntegrationTests | Linux | SQL Server and PostgreSQL containers started by the tests through Testcontainers |
| MessageTransit.DynamoDbIntegration.Tests | Linux | LocalStack DynamoDB with dummy credentials |
| MessageTransit.Azure.Table.IntegrationTests | Linux | Azurite container started by the tests through Testcontainers |
| MessageTransit.DapperIntegration.Tests | Linux | Local SQL Server |
| MessageTransit.EntityFrameworkCoreIntegration.IntegrationTests | Linux | SQL Server and PostgreSQL containers started by the tests through Testcontainers |
| MessageTransit.MartenIntegration.Tests | Linux | Local PostgreSQL |
| MessageTransit.MongoDbIntegration.IntegrationTests | Linux | Single-node MongoDB replica set container started by the tests through Testcontainers |
| MessageTransit.NHibernateIntegration.Tests | Linux | In-memory SQLite |
| MessageTransit.RedisIntegration.IntegrationTests | Two Linux jobs | Redis and Valkey containers started by the tests through Testcontainers; the Valkey job sets `MT_REDIS_SERVER=valkey` |
| MessageTransit.HangfireIntegration.Tests | Linux | In-memory storage |
| MessageTransit.QuartzIntegration.Tests | Linux | In-memory scheduler |
| MessageTransit.EventHubIntegration.IntegrationTests | Linux | Event Hubs emulator and Azurite containers started by the tests through Testcontainers; fixed host ports for the emulator |
| MessageTransit.KafkaIntegration.IntegrationTests | Linux | Kafka, ZooKeeper, and Schema Registry containers started by the tests through Testcontainers; fixed host ports |
| MessageTransit.SignalR.Tests | Linux | In-process test fixtures |
| MessageTransit.Azure.ServiceBus.Core.Tests | **Not executed** | Requires a real Azure Service Bus namespace and Azure Storage credentials; dedicated cloud test resources are not configured |
| MessageTransit.Azure.Cosmos.IntegrationTests | **Not executed** | The tests start the Cosmos DB emulator through Testcontainers on fixed host ports unless `MT_COSMOS_ENDPOINT` names an account; local runs must first trust the emulator certificate served at `https://localhost:8081/_explorer/emulator.pem`; the emulator is not run in CI, and the RBAC future fixture additionally requires Azure credentials and a remotely configured instance |

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

## Dependency advisories

Every restore runs [NuGet audit](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages) over direct and transitive packages. High and critical advisories (`NU1903`, `NU1904`) fail the restore in every project, including tests, benchmarks and build tools. Low and moderate advisories (`NU1901`, `NU1902`) stay warnings. When advisory data cannot be retrieved (`NU1900`, `NU1905`), ordinary builds warn and continue; release validation fails.

An advisory that is accepted rather than fixed is suppressed in the project or props file that resolves it, with a reason and a review date:

```xml
<ItemGroup>
  <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-xxxx-xxxx-xxxx" Reason="Why the advisory does not apply" ReviewBy="2026-12-31" />
</ItemGroup>
```

Do not hide audit warnings with `NoWarn` or `WarningsNotAsErrors`, and do not change `NuGetAudit`, `NuGetAuditMode`, `NuGetAuditLevel` or replace `WarningsAsErrors` in individual projects. The release script tests reject those changes and any suppression without both values. Release validation also fails once a review date has passed.

## Releases

Only the release workflow publishes packages, and only from `master` after a maintainer approves the inspected candidates. Each push to `master` calculates a preview version. Stable publication requires an explicit channel selection. Recovery reuses a retained candidate artifact and checks its original commit; it cannot substitute newly built files under a reserved version. The [release process](releasing.md) describes the setup, the checks, and the steps.

## Manual transport validation

`Transport Validation` is a manually dispatched workflow for RabbitMQ, ActiveMQ, and SQS/S3, using the same .NET 10 test action and local-service configuration as the main workflow. Azure Service Bus is not run because it requires a real namespace and Azure Storage credentials. SQL, Kafka, Event Hubs, and persistence suites are covered by the main workflow. No package publishing job exists in the manual workflow.

Changes to workflow definitions, the shared test action, CI helper scripts, or this matrix trigger the main workflow. Both build lanes include all tracked projects; the four currently outside the solution are the NServiceBus interoperability tests, the two benchmark executables, and the release symbol inspection tool. The release regression job also compiles real PDB fixtures to check that tool.

## Local commands

From the repository root:

```sh
python3 scripts/ci_build.py
dotnet test tests/MessageTransit.Analyzers.Tests/MessageTransit.Analyzers.Tests.csproj -c Release -f net10.0 --filter 'Category!=Flaky'
dotnet test tests/MessageTransit.Interop.NServiceBus.Tests/MessageTransit.Interop.NServiceBus.Tests.csproj -c Release -f net10.0 --filter 'Category!=Flaky'
```

Suites named `*.IntegrationTests` start their own containers through Testcontainers and need a running Docker engine; the shared containers and pinned images are in `tests/MessageTransit.Testing.Containers`. Suites with fixed host ports conflict with a local broker already listening on those ports. The remaining service-dependent suites require the corresponding local service before execution, and the workflow files contain their service configuration. CI configuration describes intended coverage; only a completed run and its retained results establish which checks actually passed.
