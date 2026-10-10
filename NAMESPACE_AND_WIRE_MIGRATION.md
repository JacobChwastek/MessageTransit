# Migrating from MassTransit to MessageTransit

MessageTransit is based on [MassTransit v8.5.10](https://github.com/MassTransit/MassTransit/tree/v8.5.10). It keeps the same programming model, but it has its own package IDs, namespaces, assembly identities, and branded wire identifiers. Migrating an application is a source-breaking change and, for running systems, a deployment cutover.

MessageTransit does not interoperate with MassTransit processes. Do not run MassTransit and MessageTransit applications against the same queues, topics, outbox tables, or scheduler stores unless you have designed and tested that coexistence yourself.

## Before you start

- **Runtime.** MessageTransit runtime packages require .NET 10. Retarget the application and its test projects first.
- **Release status.** No MessageTransit release has been published yet. Build packages from source as described in the [README](README.md#build-source-and-local-packages), and use one version for every MessageTransit package.
- **Deployment plan.** Decide whether the migrated application starts on fresh broker entities and stores, or whether existing work must be drained first. See [plan the cutover](#5-plan-the-cutover).
- **Entity Framework 6.** The EF6 integration has been removed. Move EF6 saga repositories to EF Core first, using the [EF Core migration guide](ENTITY_FRAMEWORK_MIGRATION.md).

## 1. Replace package references

Replace each `MassTransit*` package with its MessageTransit counterpart. Most IDs replace only the leading `MassTransit`, for example `MassTransit.RabbitMQ` becomes `MessageTransit.RabbitMQ`. The [package ID map](PACKAGE_IDENTITY.md#package-id-map) lists all 32 packages.

```xml
<PackageReference Include="MessageTransit" Version="0.1.0-local.1" />
<PackageReference Include="MessageTransit.RabbitMQ" Version="0.1.0-local.1" />
<PackageReference Include="MessageTransit.EntityFrameworkCore" Version="0.1.0-local.1" />
```

After restoring, confirm that no MassTransit package remains, including transitive references from your own libraries:

```bash
dotnet list package --include-transitive | grep -i masstransit
```

A MassTransit package that has no entry in the map has no MessageTransit equivalent. Libraries that depend on MassTransit, such as shared contract or plugin packages, must be rebuilt against MessageTransit before the application can use them.

## 2. Update namespaces and APIs

Every `MassTransit` segment in a namespace or a public identifier becomes `MessageTransit`. Apart from those names, types and members keep their shape. Replacing `MassTransit` with `MessageTransit` across application source is the usual starting point; review the result, because your own identifiers may contain the word too.

| MassTransit | MessageTransit |
| --- | --- |
| `using MassTransit;` and `MassTransit.*` namespaces | `using MessageTransit;` and `MessageTransit.*` namespaces |
| `AddMassTransit(...)` | `AddMessageTransit(...)` |
| `AddMassTransitTestHarness(...)`, `AddMassTransitInMemoryTestHarness(...)` | `AddMessageTransitTestHarness(...)`, `AddMessageTransitInMemoryTestHarness(...)` |
| `MassTransitStateMachine<TInstance>` | `MessageTransitStateMachine<TInstance>` |
| `MassTransitHostOptions` | `MessageTransitHostOptions` |
| `MassTransitHealthCheckOptions` | `MessageTransitHealthCheckOptions` |
| `MassTransitException`, `MassTransitApplicationException` | `MessageTransitException`, `MessageTransitApplicationException` |
| `AddMassTransitHostedService(...)`, `RemoveMassTransitHostedService(...)` | `AddMessageTransitHostedService(...)`, `RemoveMessageTransitHostedService(...)` |
| `IHostBuilder.UseMassTransit(...)` | `IHostBuilder.UseMessageTransit(...)` |
| `AddMassTransitTextWriterLogger(...)` | `AddMessageTransitTextWriterLogger(...)` |
| `AddMassTransitForAzureFunctions(...)`, `AddMassTransitEventHub(...)` | `AddMessageTransitForAzureFunctions(...)`, `AddMessageTransitEventHub(...)` |
| `MassTransitHubLifetimeManager<THub>` (SignalR) | `MessageTransitHubLifetimeManager<THub>` |
| `MassTransitMongoDbConventions` | `MessageTransitMongoDbConventions` |
| `MassTransitJsonSerializer`, `MassTransitJsonDeserializer` (Newtonsoft) | `MessageTransitJsonSerializer`, `MessageTransitJsonDeserializer` |
| `HostInfo.MassTransitVersion` | `HostInfo.MessageTransitVersion` |
| `ConfigureMassTransit(...)` in `FutureTestFixture` | `ConfigureMessageTransit(...)` |

Also update names that the compiler does not check:

- **Assembly-qualified type names** in configuration files, serialized settings, and reflection code. `MassTransit.Scheduling.ScheduleMessage, MassTransit` becomes `MessageTransit.Scheduling.ScheduleMessage, MessageTransit`.
- **Logging configuration.** Log categories start with `MessageTransit`, so a `"MassTransit": "Debug"` entry under `Logging:LogLevel` becomes `"MessageTransit": "Debug"`.
- **Analyzer suppressions.** The analyzer package is now `MessageTransit.Analyzers`. Its diagnostic IDs, such as `MTA0001`, are unchanged.

### Usage reporting

MessageTransit removes the inherited automatic usage-reporting client, including its startup hooks, report models, and configuration APIs. The library does not send usage reports to any service and does not read the `MASSTRANSIT_USAGE_TELEMETRY` environment variable.

Remove calls to `DisableUsageTelemetry()` and `ConfigureUsageTelemetryOptions(...)`, registrations of `UsageTelemetryOptions`, and code that depends on the removed usage-reporting types. There is no replacement configuration. Remove the environment variable from deployment configuration as well.

## 3. Rebuild against the new assemblies

Assembly simple names start with `MessageTransit`, and the signed assemblies use a MessageTransit strong-name key with public key token `72512384aab09b36`. MassTransit binaries, plugins, and extension libraries cannot be loaded in their place; rebuild everything that references the framework. Update any `InternalsVisibleTo`, binding, or plugin configuration that names a MassTransit assembly or its public key token.

Most assemblies have the same name as their package. These do not:

| Assembly | Package |
| --- | --- |
| `MessageTransit.ActiveMqTransport` | `MessageTransit.ActiveMQ` |
| `MessageTransit.AmazonSqsTransport` | `MessageTransit.AmazonSQS` |
| `MessageTransit.Azure.Table` | `MessageTransit.Azure.Cosmos.Table` |
| `MessageTransit.DynamoDbIntegration` | `MessageTransit.DynamoDb` |
| `MessageTransit.EntityFrameworkCoreIntegration` | `MessageTransit.EntityFrameworkCore` |
| `MessageTransit.EventHubIntegration` | `MessageTransit.EventHub` |
| `MessageTransit.HangfireIntegration` | `MessageTransit.Hangfire` |
| `MessageTransit.KafkaIntegration` | `MessageTransit.Kafka` |
| `MessageTransit.MartenIntegration` | `MessageTransit.Marten` |
| `MessageTransit.MongoDbIntegration` | `MessageTransit.MongoDb` |
| `MessageTransit.NHibernateIntegration` | `MessageTransit.NHibernate` |
| `MessageTransit.QuartzIntegration` | `MessageTransit.Quartz` |
| `MessageTransit.RabbitMqTransport` | `MessageTransit.RabbitMQ` |
| `MessageTransit.RedisIntegration` | `MessageTransit.Redis` |
| `MessageTransit.SqlTransport.PostgreSql` | `MessageTransit.SqlTransport.PostgreSQL` |
| `MessageTransit.WebJobs.EventHubsIntegration` | `MessageTransit.WebJobs.EventHubs` |
| `MessageTransit.WebJobs.ServiceBusIntegration` | `MessageTransit.WebJobs.ServiceBus` |

## 4. Review what changes on the wire

### Message identity

Message URNs are built from `urn:message:` plus the CLR namespace and type name. Your own contracts keep their identity: `Acme.Contracts.SubmitOrder` is `urn:message:Acme.Contracts:SubmitOrder` in both frameworks, and explicit `[MessageUrn]` values are honored as before.

Framework-owned contracts change identity because their namespace changes. This covers `Fault<T>`, scheduling messages, job-service messages, and routing-slip events. For example, `Fault<SubmitOrder>` was `urn:message:MassTransit:Fault[[Acme.Contracts:SubmitOrder]]` and is now `urn:message:MessageTransit:Fault[[Acme.Contracts:SubmitOrder]]`. Receivers compare these URNs with the envelope's message types, so a matching JSON shape does not make old and new messages interchangeable.

### Changed identifiers

| Identifier | MassTransit | MessageTransit |
| --- | --- | --- |
| JSON envelope media type | `application/vnd.masstransit+json` | `application/vnd.messagetransit+json` |
| Other envelope media types | `application/vnd.masstransit+xml`, `+bson`, `+msgpack`, `+aes`, `.v2+aes`, and mediator `+obj` | `application/vnd.messagetransit+xml`, `+bson`, `+msgpack`, `+aes`, `.v2+aes`, and `+obj` |
| Host version header | `MT-Host-MassTransitVersion` | `MT-Host-MessageTransitVersion` |
| Envelope host member | `"massTransitVersion"` | `"messageTransitVersion"` |
| Fault and other framework contract URNs | `urn:message:MassTransit:…` | `urn:message:MessageTransit:…` |
| Framework contract entity names, such as the RabbitMQ fault exchange | `MassTransit:Fault--Acme.Contracts:SubmitOrder--` | `MessageTransit:Fault--Acme.Contracts:SubmitOrder--` |
| `ActivitySource` and `Meter` names | `MassTransit` | `MessageTransit` |
| Metric, span attribute, and tag names | `messaging.masstransit.*` | `messaging.messagetransit.*` |
| Health check tag | `masstransit` | `messagetransit` |
| Log category prefix | `MassTransit` | `MessageTransit` |
| RabbitMQ client properties | `client_api=MassTransit`, `masstransit_version` | `client_api=MessageTransit`, `messagetransit_version` |
| Quartz durable job | `MassTransitScheduleMessageJob` | `MessageTransitScheduleMessageJob` |

### Unchanged identifiers

- The `urn:message:` prefix and the URNs of application-owned contracts.
- The `MT-` header prefix and every header other than the host version header.
- Standard media types such as `application/json` and `application/xml` used by the raw serializers.
- Default receive endpoint names. The default formatters use the consumer or saga type's simple name, so `SubmitOrderConsumer` still maps to the same queue. Formatters configured to include the namespace produce new names only when that namespace contained `MassTransit`.
- Publish entity names for application-owned contracts, and any name set explicitly with `[EntityName]` or endpoint configuration. RabbitMQ, Azure Service Bus, and Amazon SNS include the contract namespace in their default entity names, so only entities for framework-owned contracts change.

Update OpenTelemetry subscriptions (`AddSource("MessageTransit")`, `AddMeter("MessageTransit")`), dashboards, alerts, health check filters, and broker monitoring that match the old names.

## 5. Plan the cutover

Changing the framework does not rewrite work that is already stored. Broker messages, outbox rows, scheduled jobs, and job-service state written by MassTransit keep their old URNs, media types, headers, and type names, and MessageTransit does not convert them.

The simplest migration starts the new application on fresh broker entities and empty framework stores. Otherwise, stop MassTransit producers and drain or deliberately migrate each store before MessageTransit starts processing.

| Store | Fresh deployment | Retaining existing work |
| --- | --- | --- |
| Broker queues, topics, subscriptions, delayed and error messages | Use a new virtual host or namespace, or verify that reused entities hold no MassTransit messages. | Stop old producers, drain or transform pending messages, and review entity and subscription names. Framework contract entities such as fault exchanges are new. |
| EF Core and MongoDB inbox and outbox | Start with empty tables or collections. | Deliver pending outbox messages before switching, or migrate the stored body, media type, message types, addresses, and headers. Keep inbox consumer identity stable to avoid duplicate processing. |
| Application sagas | Provision new storage with the chosen persistence provider. | Saga state is your own type and usually keeps its storage. Compare table or collection names, discriminators, and member names, and check for outstanding events produced by MassTransit. |
| Quartz, Hangfire, and job-service state | Schedule new work through MessageTransit. | Let scheduled work complete, or cancel and re-create it. The Quartz job is registered under a new name, and stored messages and job types carry MassTransit identities. |

The MongoDB class map conventions recognize framework types by the `MessageTransit` namespace and still apply to saga classes; application documents are otherwise unaffected.

## Migration checklist

1. Retarget to .NET 10 and replace every MassTransit package with its MessageTransit counterpart at one version.
2. Rebuild libraries, plugins, and test helpers that reference the framework; confirm no MassTransit package remains transitively.
3. Rename namespaces and `MassTransit`-bearing APIs; update assembly-qualified names, logging configuration, and removed usage-reporting calls.
4. Update telemetry subscriptions, dashboards, health check filters, and broker monitoring.
5. Decide on fresh infrastructure or drain existing work, then verify the resulting broker topology and stores.
6. Run the application's tests and an end-to-end message flow before switching production traffic.
