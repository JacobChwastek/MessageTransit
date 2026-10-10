# MessageTransit package identity

MessageTransit is an independently maintained fork of [MassTransit v8.5.10](https://github.com/MassTransit/MassTransit/tree/v8.5.10). The package names below are the fork's package IDs, and the source projects now evaluate with them; this page does not announce published MessageTransit packages.

## Package ID map

All 32 retained source projects currently evaluate as packable. Each planned ID replaces the leading `MassTransit` in the **effective NuGet package ID**, preserving the rest of that ID. This keeps a direct migration path for packages whose ID differs from the project filename. The planned IDs are distinct without changing any upstream package.

| Current package ID | Planned package ID |
| --- | --- |
| `MassTransit` | `MessageTransit` |
| `MassTransit.Abstractions` | `MessageTransit.Abstractions` |
| `MassTransit.Analyzers` | `MessageTransit.Analyzers` |
| `MassTransit.Interop.NServiceBus` | `MessageTransit.Interop.NServiceBus` |
| `MassTransit.MessagePack` | `MessageTransit.MessagePack` |
| `MassTransit.Newtonsoft` | `MessageTransit.Newtonsoft` |
| `MassTransit.SignalR` | `MessageTransit.SignalR` |
| `MassTransit.StateMachineVisualizer` | `MessageTransit.StateMachineVisualizer` |
| `MassTransit.TestFramework` | `MessageTransit.TestFramework` |
| `MassTransit.AmazonS3` | `MessageTransit.AmazonS3` |
| `MassTransit.Azure.Cosmos` | `MessageTransit.Azure.Cosmos` |
| `MassTransit.Azure.Storage` | `MessageTransit.Azure.Storage` |
| `MassTransit.Azure.Cosmos.Table` | `MessageTransit.Azure.Cosmos.Table` |
| `MassTransit.DapperIntegration` | `MessageTransit.DapperIntegration` |
| `MassTransit.DynamoDb` | `MessageTransit.DynamoDb` |
| `MassTransit.EntityFrameworkCore` | `MessageTransit.EntityFrameworkCore` |
| `MassTransit.Marten` | `MessageTransit.Marten` |
| `MassTransit.MongoDb` | `MessageTransit.MongoDb` |
| `MassTransit.NHibernate` | `MessageTransit.NHibernate` |
| `MassTransit.Redis` | `MessageTransit.Redis` |
| `MassTransit.Hangfire` | `MessageTransit.Hangfire` |
| `MassTransit.Quartz` | `MessageTransit.Quartz` |
| `MassTransit.ActiveMQ` | `MessageTransit.ActiveMQ` |
| `MassTransit.AmazonSQS` | `MessageTransit.AmazonSQS` |
| `MassTransit.Azure.ServiceBus.Core` | `MessageTransit.Azure.ServiceBus.Core` |
| `MassTransit.EventHub` | `MessageTransit.EventHub` |
| `MassTransit.Kafka` | `MessageTransit.Kafka` |
| `MassTransit.RabbitMQ` | `MessageTransit.RabbitMQ` |
| `MassTransit.SqlTransport.PostgreSQL` | `MessageTransit.SqlTransport.PostgreSQL` |
| `MassTransit.SqlTransport.SqlServer` | `MessageTransit.SqlTransport.SqlServer` |
| `MassTransit.WebJobs.EventHubs` | `MessageTransit.WebJobs.EventHubs` |
| `MassTransit.WebJobs.ServiceBus` | `MessageTransit.WebJobs.ServiceBus` |

The candidate prerelease family contains these 32 IDs. Retaining a source project does not require publishing it before its build, dependencies, license materials, and support scope are validated. The EF6 integration has been removed, and no `MessageTransit.EntityFramework` package is planned. Use the retained EF Core integration and follow the [migration guide](ENTITY_FRAMEWORK_MIGRATION.md).

## NuGet name check and ownership

On **2026-10-07**, all 33 proposed IDs returned no versions from NuGet's [package content API](https://learn.microsoft.com/en-us/nuget/api/package-base-address-resource), no registration record, and no exact `packageid:` search result. This includes the root [`MessageTransit` content index](https://api.nuget.org/v3-flatcontainer/messagetransit/index.json). NuGet's content API includes unlisted versions, so this check covers more than the gallery search alone. It is a dated observation, not a reservation or proof that a particular account can publish.

NuGet package IDs are [case-insensitive and unique](https://learn.microsoft.com/en-us/nuget/reference/nuspec#id). A reserved prefix can restrict who may publish an unused ID. Before publication, recheck the exact IDs, confirm the publishing account's rights, and consider a [prefix reservation](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation) that explicitly covers both `MessageTransit` and `MessageTransit.*`. Do not publish placeholder packages merely to hold names; NuGet's [deletion and prohibited-use policy](https://learn.microsoft.com/en-us/nuget/nuget-org/policies/deleting-packages) rules out package squatting.

## Current package evidence

On **2026-10-07**, MSBuild evaluation of all 33 source projects found `Version=1.0.0`, `Product=MassTransit`, `PackageProjectUrl=https://masstransit.io`, `PackageIcon=mt-logo-small.png`, and `PackageReadmeFile=NuGet.README.md`. The evaluated `RepositoryUrl` property was empty. A local Release pack of the core project succeeded and produced `MassTransit.1.0.0.nupkg` with `netstandard2.0`, `net8.0`, `net9.0`, and `net10.0` assemblies. Its metadata and contents confirmed the upstream project URL, icon, README, description, tags, and author string. The package already carries copyright credit for both Chris Patterson and Jacob Chwastek. The package's repository metadata **did** resolve to the MessageTransit GitHub fork through the build's source-link integration; an empty evaluated `RepositoryUrl` did not make the packed repository URL empty.

The checked package was a local artifact, not a MessageTransit release candidate. The GitHub workflow separately overrides package versions from `MASSTRANSIT_VERSION: 8.5.10`. A package produced by that workflow therefore cannot be assumed to have the local `1.0.0` version. The final package manifest and every `.nupkg` must be inspected after the coordinated rename.

## Version series

Use one independent version for packages released together, beginning with **`0.1.0-preview.1`**. Advance the prerelease number for each published candidate, then use `0.1.0` only when the fork is ready for its first stable release. This version does not continue MassTransit's `8.5.10` line; that number identifies the upstream source baseline. [NuGet versions](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning) are immutable once published, and build metadata such as `+sha` does not provide a distinct normalized package version.

The release workflow currently calculates versions from `MASSTRANSIT_VERSION: 8.5.10`. It must use the MessageTransit series and an approved package manifest before publishing. Package references among released MessageTransit packages should resolve to the coordinated MessageTransit version.

## Public-facing metadata and artwork

The metadata decisions for the coordinated rename are:

- Use the package IDs above, `Product=MessageTransit`, fork-specific titles, descriptions and tags, and the [MessageTransit repository](https://github.com/JacobChwastek/MessageTransit) as the project URL until a separate site exists. Verify that the packed repository URL still points to the fork.
- Add Jacob Chwastek to package author metadata while retaining existing contributor credits where applicable. Package authors are descriptive metadata; NuGet publishing rights depend on the account's actual package ownership.
- Replace the packed root `NuGet.README.md` and the public repository README with accurate fork installation, support, and package information. The repository keeps a single packed `NuGet.README.md`. Remove inherited upstream CI badges and support links from fork-facing claims.
- Omit the NuGet icon until an original MessageTransit asset is ready. The package build no longer packs the inherited `mt-logo-small.png`. The repository credits its artwork to The Agile Badger, but no separate permission for presenting it as MessageTransit branding has been established here.
- Keep `LICENSE`, `NOTICE`, `COPYRIGHT`, `THIRD-PARTY-NOTICES`, and applicable source attribution. [Apache-2.0 section 6](https://www.apache.org/licenses/LICENSE-2.0.html) does not grant general rights to use upstream trade names or marks as a new product identity.

The exact `MessageTransit` name has not received a formal trademark clearance. A broad web search and NuGet search are insufficient to establish that; [WIPO](https://www.wipo.int/en/web/global-brand-database) and [EUIPO](https://www.euipo.europa.eu/en/trade-marks/before-applying/availability) describe the relevant registry searches. This remains a publication decision for the maintainer.

The source uses these package IDs together with MessageTransit namespaces, assembly names, and message identities. The release pipeline still needs the MessageTransit version series, and every packed `.nupkg` must be inspected before the first package is published.
