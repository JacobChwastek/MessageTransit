# MessageTransit namespace and wire migration

This is the migration contract for the first MessageTransit release. The source uses `MessageTransit` APIs and wire identifiers. MessageTransit uses its own public namespace and assembly identity, and the first release will not promise interoperability with existing MassTransit processes or queued messages. The [package identity map](PACKAGE_IDENTITY.md) defines the corresponding NuGet IDs and the independent prerelease version series.

## Identity decisions

| Surface | Planned treatment | Consequence |
| --- | --- | --- |
| Fork-owned C# namespaces | Replace the leading `MassTransit` namespace segment with `MessageTransit`, preserving suffixes. Rename public identifiers that contain the old product name, such as `AddMassTransit`, `MassTransitStateMachine<T>`, and `MassTransitHostOptions`. | Source references, `using` directives, reflection strings, and extension method calls must change. Do not rename unrelated `System`, `NServiceBus`, or vendored namespaces. |
| Assemblies and signing | Give source assemblies `MessageTransit` simple names. Keep the present signed/unsigned project split, but use a distinct fork strong-name key for signed assemblies when the rename is implemented. | Existing binaries and plugins cannot be used as drop-in replacements, even when an API shape looks similar. Merely renaming the current `.snk` file would retain its public key token. |
| Packages | Replace each effective package ID using the published [old-to-new map](PACKAGE_IDENTITY.md#package-id-map); keep assembly and package IDs as separate mappings. | All direct and transitive fork package references must use one coordinated MessageTransit version. |
| Compatibility shims | Do not ship an old `MassTransit` namespace facade, type forwarders, a legacy URN registry, or automatic durable-data conversion in the first release. | Rebuild consumers and extensions. Any deployment retaining old work requires a separately specified cutover. |
| Third-party names and attribution | Keep third-party protocol names, source attribution, and legal notices. | A residual `MassTransit` search must classify legal/provenance text rather than delete it indiscriminately. |

All 32 source projects currently derive their assembly simple name from the project filename. Rename the leading `MassTransit` segment of each assembly name, preserving its suffix. **Seventeen assemblies have a different suffix from their NuGet package ID**; their planned names are:

| Current assembly | Planned assembly | Planned NuGet ID |
| --- | --- | --- |
| `MassTransit.Azure.Table` | `MessageTransit.Azure.Table` | `MessageTransit.Azure.Cosmos.Table` |
| `MassTransit.DynamoDbIntegration` | `MessageTransit.DynamoDbIntegration` | `MessageTransit.DynamoDb` |
| `MassTransit.EntityFrameworkCoreIntegration` | `MessageTransit.EntityFrameworkCoreIntegration` | `MessageTransit.EntityFrameworkCore` |
| `MassTransit.MartenIntegration` | `MessageTransit.MartenIntegration` | `MessageTransit.Marten` |
| `MassTransit.MongoDbIntegration` | `MessageTransit.MongoDbIntegration` | `MessageTransit.MongoDb` |
| `MassTransit.NHibernateIntegration` | `MessageTransit.NHibernateIntegration` | `MessageTransit.NHibernate` |
| `MassTransit.RedisIntegration` | `MessageTransit.RedisIntegration` | `MessageTransit.Redis` |
| `MassTransit.HangfireIntegration` | `MessageTransit.HangfireIntegration` | `MessageTransit.Hangfire` |
| `MassTransit.QuartzIntegration` | `MessageTransit.QuartzIntegration` | `MessageTransit.Quartz` |
| `MassTransit.ActiveMqTransport` | `MessageTransit.ActiveMqTransport` | `MessageTransit.ActiveMQ` |
| `MassTransit.AmazonSqsTransport` | `MessageTransit.AmazonSqsTransport` | `MessageTransit.AmazonSQS` |
| `MassTransit.EventHubIntegration` | `MessageTransit.EventHubIntegration` | `MessageTransit.EventHub` |
| `MassTransit.KafkaIntegration` | `MessageTransit.KafkaIntegration` | `MessageTransit.Kafka` |
| `MassTransit.RabbitMqTransport` | `MessageTransit.RabbitMqTransport` | `MessageTransit.RabbitMQ` |
| `MassTransit.SqlTransport.PostgreSql` | `MessageTransit.SqlTransport.PostgreSql` | `MessageTransit.SqlTransport.PostgreSQL` |
| `MassTransit.WebJobs.EventHubsIntegration` | `MessageTransit.WebJobs.EventHubsIntegration` | `MessageTransit.WebJobs.EventHubs` |
| `MassTransit.WebJobs.ServiceBusIntegration` | `MessageTransit.WebJobs.ServiceBusIntegration` | `MessageTransit.WebJobs.ServiceBus` |

The remaining 15 assembly names follow the corresponding package ID. The current signing configuration enables strong-name signing for 24 of the 32 assemblies through [`signing.props`](signing.props); eight are unsigned. The EF6 assembly is retired; see the [migration guide](ENTITY_FRAMEWORK_MIGRATION.md). The later rename must inspect actual assembly names, public key tokens, package dependency IDs, and analyzer DLL paths rather than infer them from filenames alone.

## Message and broker identity

The [`MessageUrn`](src/MessageTransit.Abstractions/MessageUrn.cs) generator uses `urn:message:` plus a CLR type's namespace and name, recursively including generic arguments. An explicit [`MessageUrn` attribute](src/MessageTransit.Abstractions/Attributes/MessageUrnAttribute.cs) supplies an alternative. Keep the neutral `urn:message:` prefix and preserve application-owned contract namespaces, names, and explicit URNs unless the application deliberately changes them. Replacing only the framework package and API namespace does not change a contract such as `Acme.Contracts.SubmitOrder`.

Framework-owned contracts **do** change identity when their namespace changes. That includes `Fault<T>`, scheduling messages, job-service messages, and routing-slip contracts. A generic `Fault<Acme.Contracts.SubmitOrder>` gains a new wrapper URN even when the application contract URN remains stable. The envelope records supported message URNs, and [`EnvelopeSerializerContext`](src/MessageTransit/Serialization/EnvelopeSerializerContext.cs) compares them with the receiving type's computed URN. Matching JSON shape is therefore not enough to promise old/new consumption. Raw serializers have different type-acceptance behavior; they do not create a general compatibility guarantee.

Use the following protocol treatments during implementation:

| Identifier | Planned treatment | Required check |
| --- | --- | --- |
| `application/vnd.masstransit+json`, `+msgpack`, `+xml`, `+bson`, `+aes`, `.v2+aes`, and mediator `+obj` | Give branded media types the corresponding `application/vnd.messagetransit…` values. Keep standard/raw media types such as `application/json`. | Inspect every serializer registration, emitted content type, and decoder path. An unknown legacy media type can fall back to a default decoder; interoperability is still unsupported. |
| `MT-*` headers | Keep the `MT-` prefix and neutral existing keys. Rename the explicit `MT-Host-MassTransitVersion` key to `MT-Host-MessageTransitVersion`. | Verify send, receive, outbox, error/move transports, fault, retry, and scheduling header propagation. |
| `HostInfo.MassTransitVersion` and serialized host version | Rename to `MessageTransitVersion`, including the camel-case envelope member. | Compare serialized envelopes and any NServiceBus adapter mapping. |
| Default receive endpoint names | Retain the current simple-type-name algorithm. A namespace-only change does not rename a default consumer queue when its simple class name is unchanged. | Compare endpoint names before/after for the same consumer and saga types. Explicit names and changed simple names require separate review. |
| Publish exchange/topic/entity names | Retain each transport's naming algorithm. RabbitMQ, Azure Service Bus, and Amazon SQS/SNS include contract namespaces by default, so framework-owned renamed contracts can acquire new entities. | Compare entity and subscription names on each retained transport; preserve application-owned contract names or explicit `[EntityName]` overrides deliberately. |
| Activity source, Meter, custom metric/tag names, RabbitMQ client metadata | Publish the new `MessageTransit`, `messaging.messagetransit.*`, and `messagetransit_version` identifiers. | Update instrumentation subscriptions, dashboards, alerts, WebJobs activity listeners, and client metadata checks together. |

The receive endpoint behavior follows [`DefaultEndpointNameFormatter`](src/MessageTransit/Configuration/DefaultEndpointNameFormatter.cs). Publish entity names follow [`DefaultMessageNameFormatter`](src/MessageTransit/Transports/DefaultMessageNameFormatter.cs) and each transport's formatter. The branded media types are defined in the core, Newtonsoft, and MessagePack serializer projects. `MT-*` keys live in [`MessageHeaders`](src/MessageTransit.Abstractions/MessageHeaders.cs); telemetry names live in [`DiagnosticHeaders`](src/MessageTransit/Logging/Diagnostics/DiagnosticHeaders.cs) and [`ConfigureDefaultInstrumentationOptions`](src/MessageTransit/Monitoring/ConfigureDefaultInstrumentationOptions.cs).

## Usage reporting

MessageTransit removes the inherited automatic usage-reporting client, including its startup hooks, report models, and configuration APIs. The library no longer sends usage reports to the upstream service or reads the `MASSTRANSIT_USAGE_TELEMETRY` environment variable.

When migrating an application, remove calls to `DisableUsageTelemetry()` and `ConfigureUsageTelemetryOptions(...)`, registrations of `UsageTelemetryOptions`, and custom code that depends on the removed usage-reporting types. There is no replacement configuration to add. Remove the obsolete environment variable from deployment configuration as well.

Application observability remains available through MessageTransit's OpenTelemetry-compatible activity sources and metrics. Configure those subscriptions and exporters in the application using the renamed instrumentation identifiers above.

## Stored work and cutover

The initial release assumes a fresh MessageTransit deployment or an explicitly reviewed cutover. It does not replay retained MassTransit broker messages, outbox rows, scheduled jobs, or framework job state automatically. Existing deployments must inventory and drain or migrate those stores **before** switching; changing CLR namespaces does not rewrite persisted addresses, URNs, content types, headers, job types, or consumer identities.

| Store | Fresh deployment | Retained data requires |
| --- | --- | --- |
| Broker queues, topics, subscriptions, delayed and error messages | Use a fresh broker namespace/vhost or verify that reused entities contain no old work; then verify the intended topology. | Stop old producers, capture topology, drain or deliberately transform pending messages, and validate names and type headers. Do not blindly rename broker entities. |
| EF Core/Mongo inbox and outbox | Start empty stores. | Drain pending deliveries or design a data migration for stored body, media type, supported URNs, addresses and headers. Preserve or remap inbox consumer identity to avoid duplicate-processing changes. |
| Application sagas | Provision fresh stores using the chosen persistence provider. | Compare state/member names, discriminators, table or collection names, and outstanding events. A namespace rename does not universally rename storage keys. |
| Quartz, Hangfire, and job-service state | Schedule new work through renamed contracts and types. | Drain/cancel/export old work or migrate serialized CLR job identity, message body, headers, and job type IDs with provider-specific checks. |

The outbox persists complete message metadata in [`EntityFrameworkOutboxExtensions`](src/Persistence/MessageTransit.EntityFrameworkCoreIntegration/EntityFrameworkCoreIntegration/EntityFrameworkOutboxExtensions.cs) and [`MongoDbOutboxExtensions`](src/Persistence/MessageTransit.MongoDbIntegration/MongoDbIntegration/Outbox/MongoDbOutboxExtensions.cs). Quartz has the durable job identity `MassTransitScheduleMessageJob` in [`ScheduleMessageConsumer`](src/Scheduling/MessageTransit.QuartzIntegration/QuartzIntegration/ScheduleMessageConsumer.cs); rename it to `MessageTransitScheduleMessageJob` for fresh stores. MongoDB convention registration also tests whether a type's full name begins with `MassTransit` in [`MassTransitMongoDbConventions`](src/Persistence/MessageTransit.MongoDbIntegration/MessageTransitMongoDbConventions.cs), so the namespace change requires a semantic update there. Do not infer that every saga table or collection name changes: some formatters use the simple type name or explicit configuration.

## Consumer migration outline

1. Target the supported .NET 10 runtime and replace direct package references with the matching MessageTransit IDs and one coordinated version. Rebuild every application, extension package, plugin, and test helper against the new assemblies; inspect transitive dependencies for old MassTransit packages.
2. Update `using` and fully qualified namespaces; renamed registration methods, state-machine bases, host options, exceptions, testing helpers, health checks, and integration adapters; and assembly-qualified configuration or reflection strings. Keep application contract names stable unless deliberately changing their message identity.
3. Reconfigure telemetry subscriptions and any explicit endpoint/entity names that should change. Check analyzer diagnostics and code fixes against the new namespace; loading the renamed analyzer DLL alone is insufficient.
4. For a fresh deployment, start with empty broker and durable stores and verify the new topology. For retained work, complete a provider-specific drain or migration before cutover. Do not run old and new framework processes against the same queues or durable state without a separately tested interoperability design.
5. Publish an explicit breaking-change guide with the release. A successful source rebuild does not prove broker or persisted-data compatibility.

## Rename and verification checklist

- Rename source and test project paths, solution entries, assembly names, `RootNamespace` values, C# namespaces, public `MassTransit`-bearing identifiers, project references, and package IDs together. Update source-generated and reflection-based names, XML mappings, resource names, examples, and build scripts. Retain legal and third-party attributions.
- Update the analyzer's hard-coded namespace and symbol recognition tables; verify diagnostics and code fixes in a clean .NET 10 consumer. Keep its compiler-host target decision separate from runtime target changes.
- Sign the 24 signed assemblies with a distinct fork strong-name key; verify compiled public key tokens and leave the eight unsigned assemblies unsigned unless a separate policy changes them.
- Compare generated URNs for unchanged application contracts and renamed built-in contracts, including generic faults, scheduling, jobs, and routing slips. Compare serialized envelope members, each branded media type, headers, transport entities, and default/explicit endpoint names.
- Exercise fresh outbox delivery and inbox deduplication, EF Core/Mongo conventions, saga persistence, Quartz/Hangfire scheduling, and NServiceBus header/type mapping. The NServiceBus adapter uses assembly-qualified type identities, so its third-party protocol names stay while its fork type references change.
- Build and pack the approved project matrix; inspect every `.nuspec`, assembly simple name and token, analyzer DLL, and package dependency ID. Compile a clean .NET 10 consumer using the renamed registration API, consumer, state machine, and test harness. Search for remaining `MassTransit` references and classify legal/provenance text separately from active API or protocol identity.

These are the acceptance checks for the rename. A successful source build does not by itself prove broker or persisted-data compatibility.
