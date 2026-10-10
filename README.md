# MessageTransit

MessageTransit is an open-source framework for .NET applications that communicate through messages. It provides consumers, sagas and state machines, transport integrations, scheduling, and a test harness.

This repository is an independently maintained fork of [MassTransit v8.5.10](https://github.com/MassTransit/MassTransit/tree/v8.5.10). MessageTransit has its own namespaces, assembly names, signing key, and package IDs. Migrating an application requires source changes and a review of message and stored-data compatibility; see [namespace and wire migration](NAMESPACE_AND_WIRE_MIGRATION.md).

## Requirements and availability

The runtime libraries require **.NET 10**. Building the repository requires the .NET 10 SDK selected by `global.json`. `MessageTransit.Analyzers` is compiler tooling targeting **.NET Standard 2.0**, which does not extend the runtime libraries' supported frameworks.

The package list below describes the 32 retained source projects. It does not announce a published or supported MessageTransit release. Use a source checkout or locally built packages while release availability and support are being established. The intended first public version series is `0.1.0-preview.1`; `8.5.10` identifies the upstream source baseline.

## Package family

| Area | Package IDs |
| --- | --- |
| Core | `MessageTransit`, `MessageTransit.Abstractions` |
| Serialization | `MessageTransit.Newtonsoft`, `MessageTransit.MessagePack` |
| Compiler tooling | `MessageTransit.Analyzers` |
| Testing and visualization | `MessageTransit.TestFramework`, `MessageTransit.StateMachineVisualizer` |
| Integrations | `MessageTransit.SignalR`, `MessageTransit.Interop.NServiceBus` |
| Persistence and message data | `MessageTransit.AmazonS3`, `MessageTransit.Azure.Cosmos`, `MessageTransit.Azure.Storage`, `MessageTransit.Azure.Cosmos.Table`, `MessageTransit.DapperIntegration`, `MessageTransit.DynamoDb`, `MessageTransit.EntityFrameworkCore`, `MessageTransit.Marten`, `MessageTransit.MongoDb`, `MessageTransit.NHibernate`, `MessageTransit.Redis` |
| Scheduling | `MessageTransit.Hangfire`, `MessageTransit.Quartz` |
| Transports | `MessageTransit.ActiveMQ`, `MessageTransit.AmazonSQS`, `MessageTransit.Azure.ServiceBus.Core`, `MessageTransit.RabbitMQ`, `MessageTransit.SqlTransport.PostgreSQL`, `MessageTransit.SqlTransport.SqlServer` |
| Riders | `MessageTransit.EventHub`, `MessageTransit.Kafka` |
| Azure WebJobs | `MessageTransit.WebJobs.EventHubs`, `MessageTransit.WebJobs.ServiceBus` |

Package IDs can differ from project and assembly names. In particular, the Azure Table project produces `MessageTransit.Azure.Cosmos.Table`, and the Dapper package is `MessageTransit.DapperIntegration`. See the [package identity map](PACKAGE_IDENTITY.md) for the complete migration mapping. The EF6 integration has been removed; use the [EF Core migration guide](ENTITY_FRAMEWORK_MIGRATION.md) when migrating persistence.

## Build source and local packages

Run these commands from a shell with the .NET 10 SDK available:

```bash
git clone https://github.com/JacobChwastek/MessageTransit.git
cd MessageTransit
dotnet restore MessageTransit.slnx
dotnet build MessageTransit.slnx --configuration Release --no-restore -m:1 -p:Version=0.1.0-local.1
dotnet pack MessageTransit.slnx --configuration Release --no-build --no-restore -p:Version=0.1.0-local.1 --output ./artifacts
```

The build and pack commands use the same illustrative local version so package dependencies remain coordinated. These commands create local artifacts; they do not publish them. The solution covers the runtime library and analyzer projects. See [CI coverage and local commands](CI.md) for additional projects and verification requirements; integration suites require their corresponding services.

For an existing .NET 10 consumer project, add a local package reference and restore using both the local package directory and a source for third-party dependencies. Substitute the actual consumer project and absolute artifact path:

```bash
dotnet add Consumer.csproj package MessageTransit --version 0.1.0-local.1 --no-restore
dotnet restore Consumer.csproj --source "/absolute/path/to/MessageTransit/artifacts" --source https://api.nuget.org/v3/index.json
```

Add the transport and persistence packages your application uses at the same local version. For source references, reference the appropriate renamed project under `src/`.

## Getting started

Choose one transport package, plus any persistence or scheduling packages the application needs, and reference them at the same version as `MessageTransit`. This worker service uses RabbitMQ; it references `MessageTransit.RabbitMQ` and `Microsoft.Extensions.Hosting`:

```csharp
using Acme.Contracts;
using MessageTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMessageTransit(x =>
{
    x.AddConsumer<SubmitOrderConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Build().Run();
```

Message contracts must be declared in a namespace. A consumer implements `IConsumer<T>`:

```csharp
namespace Acme.Contracts;

using MessageTransit;

public record SubmitOrder(Guid OrderId);

public class SubmitOrderConsumer : IConsumer<SubmitOrder>
{
    public Task Consume(ConsumeContext<SubmitOrder> context) => Task.CompletedTask;
}
```

`ConfigureEndpoints` creates a receive endpoint for each registered consumer, named from the consumer type. Use `UsingInMemory` in place of a broker for local experiments, and `AddMessageTransitTestHarness` from the core package in tests.

For .NET 10 async sequence operators such as `Take` and `ToListAsync`, use `System.Linq`. Message-list helpers that handle timeouts and cancellation remain available.

## Migrating from MassTransit

Applications built on MassTransit 8.x must replace packages, rename namespaces and APIs such as `AddMassTransit` and `MassTransitStateMachine<T>`, and plan a cutover for queued messages and stored framework data. Follow the [migration guide](NAMESPACE_AND_WIRE_MIGRATION.md), which lists the renamed APIs and wire identifiers. Applications using the EF6 saga repository should first follow the [EF Core migration guide](ENTITY_FRAMEWORK_MIGRATION.md).

## Verified integrations

Continuous integration builds every project on Linux and Windows and runs these suites on Linux. The core, abstractions, analyzer, and NServiceBus suites also run on Windows. Container-based suites start their own services through Testcontainers.

| Area | Exercised in CI |
| --- | --- |
| Core, serialization, analyzers, test harness | In-memory transport and mediator; System.Text.Json, Newtonsoft, and MessagePack serializers; analyzers; state machine visualizer |
| Transports | RabbitMQ, ActiveMQ, Amazon SQS and SNS (LocalStack), SQL Server and PostgreSQL SQL transport |
| Riders | Kafka, Azure Event Hubs (emulator) |
| Persistence | EF Core (SQL Server and PostgreSQL), MongoDB, Redis and Valkey, Dapper (SQL Server), Marten (PostgreSQL), NHibernate (SQLite), DynamoDB (LocalStack), Azure Table (Azurite), Amazon S3 message data (LocalStack) |
| Scheduling and integrations | Quartz, Hangfire, SignalR, NServiceBus interoperability |

These packages are built but not exercised: Azure Service Bus, Azure Cosmos DB, Azure Storage message data, and both Azure WebJobs packages. Emulator results do not establish compatibility with the live Azure services. Tests marked `Flaky` are excluded from unattended runs. [CI coverage and local commands](CI.md) has the per-project matrix and exclusions.

## Support and contributing

Read [MessageTransit support guidance](SUPPORT.md) for current contact availability, reproduction details, and logging instructions. Issues and Discussions are currently disabled for this repository. Read the [security policy](SECURITY.md) before sharing a suspected vulnerability; private vulnerability reporting is also currently unavailable.

To propose a change, read the [contribution guide](CONTRIBUTING.md) for build, test, and pull request expectations.

## Provenance, license, and attribution

MessageTransit is an independently maintained derivative of MassTransit, based on the `v8.5.10` tag. It is not an official MassTransit distribution. The upstream Git history is retained in this repository; the [provenance and support decision](docs/decisions/0001-fork-provenance-and-support.md) records the exact source commit.

MessageTransit retains the upstream Apache-2.0 license and contributor attribution. Some bundled components also carry MIT terms. See [LICENSE](LICENSE), [NOTICE](NOTICE), [COPYRIGHT](COPYRIGHT), [THIRD-PARTY-NOTICES](THIRD-PARTY-NOTICES), and the [redistribution notes](docs/legal/redistribution-inventory.md).

The inherited upstream logo artwork is credited to The Agile Badger.
