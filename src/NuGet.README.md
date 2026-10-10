# MessageTransit

MessageTransit provides message-based application components for .NET: consumers, sagas and state machines, transport integrations, scheduling, and a test harness.

This is an independently maintained fork of [MassTransit v8.5.10](https://github.com/MassTransit/MassTransit/tree/v8.5.10). It uses MessageTransit namespaces, assembly identities, a distinct signing key, and package IDs. Existing applications must follow the [namespace and wire migration guide](https://github.com/JacobChwastek/MessageTransit/blob/develop/NAMESPACE_AND_WIRE_MIGRATION.md) before adopting the fork.

## Requirements and release availability

Runtime libraries require **.NET 10**. Build source with a **.NET 10 SDK** compatible with the repository's `global.json`. `MessageTransit.Analyzers` targets **.NET Standard 2.0** as compiler tooling; runtime libraries still require .NET 10.

The following 32 package IDs correspond to retained source projects. This README does not announce a published or supported MessageTransit release. Use source references or locally built packages while release availability and support are being established. The intended first public series begins with `0.1.0-preview.1`.

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

The effective Azure Table package ID is `MessageTransit.Azure.Cosmos.Table`; the Dapper package ID is `MessageTransit.DapperIntegration`. Package IDs and assembly names may differ. See the [package identity map](https://github.com/JacobChwastek/MessageTransit/blob/develop/PACKAGE_IDENTITY.md) for all old-to-new package mappings. The EF6 integration has been removed; see [migration to EF Core](https://github.com/JacobChwastek/MessageTransit/blob/develop/ENTITY_FRAMEWORK_MIGRATION.md).

## Build and consume local packages

Clone the source and build and pack the solution with one coordinated local version:

```bash
git clone https://github.com/JacobChwastek/MessageTransit.git
cd MessageTransit
dotnet restore MessageTransit.slnx
dotnet build MessageTransit.slnx --configuration Release --no-restore -m:1 -p:Version=0.1.0-local.1
dotnet pack MessageTransit.slnx --configuration Release --no-build --no-restore -p:Version=0.1.0-local.1 --output ./artifacts
```

The example version `0.1.0-local.1` identifies local artifacts. These commands do not publish packages. Add the packages needed by your .NET 10 application at that version. For an existing consumer, replace the project name and artifact path below:

```bash
dotnet add Consumer.csproj package MessageTransit --version 0.1.0-local.1 --no-restore
dotnet restore Consumer.csproj --source "/absolute/path/to/MessageTransit/artifacts" --source https://api.nuget.org/v3/index.json
```

The second source supplies third-party dependencies. Add transport or persistence packages at the same version, or reference the corresponding renamed projects under `src/`. Use `MessageTransit` namespaces and the `AddMessageTransit` registration API. Read the migration guide before reusing existing message queues or persisted state.

## Support and security

[Support guidance](https://github.com/JacobChwastek/MessageTransit/blob/develop/SUPPORT.md) describes the current contact limitations, how to prepare a reproduction, and how to collect application logs. GitHub Issues and Discussions are currently disabled for this repository.

Read the [security policy](https://github.com/JacobChwastek/MessageTransit/blob/develop/SECURITY.md) before sharing vulnerability information. Private vulnerability reporting is currently unavailable; no confidential reporting channel is advertised.

## Source and licenses

The source repository is [JacobChwastek/MessageTransit](https://github.com/JacobChwastek/MessageTransit). The fork retains upstream Apache-2.0 licensing and contributor attribution; some bundled components also carry MIT terms. The package includes applicable license and notice files. The repository also provides [LICENSE](https://github.com/JacobChwastek/MessageTransit/blob/develop/LICENSE), [NOTICE](https://github.com/JacobChwastek/MessageTransit/blob/develop/NOTICE), [COPYRIGHT](https://github.com/JacobChwastek/MessageTransit/blob/develop/COPYRIGHT), and [THIRD-PARTY-NOTICES](https://github.com/JacobChwastek/MessageTransit/blob/develop/THIRD-PARTY-NOTICES).
