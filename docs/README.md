# MessageTransit documentation

Start with the repository [README](../README.md) for requirements, the package family, and a getting-started example.

## Migrating applications

- [Namespace and wire migration](migration/namespace-and-wire.md): moving a MassTransit 8.x application to MessageTransit, including renamed APIs, message identities, and stored data.
- [EF Core migration](migration/entity-framework-core.md): replacing the removed EF6 saga repository and audit store with EF Core.
- [Marten 9 upgrade](migration/marten.md): the Marten 9 changes that affect saga storage, including the default serializer.
- [StackExchange.Redis 3 upgrade](migration/redis.md): the StackExchange.Redis 3 changes that affect Redis saga storage and the applications that use it.

## Quartz scheduling

`MessageTransit.Quartz` uses Quartz 4 as its supported scheduler baseline, currently
version 4.5.1. `UseInMemoryScheduler()` creates its own scheduler. To configure the
scheduler yourself, build a factory with `QuartzSchedulerBuilder` and pass it to
`UseInMemoryScheduler`, or register the scheduler with `AddQuartz()` and add
`AddQuartzConsumers()`.

When the bus stops, it shuts down a scheduler it created. A scheduler from your own
factory goes into standby, so the bus can start again, and shuts down when you dispose
the factory. With `AddQuartzConsumers()`, the scheduler shuts down when the service
provider is disposed.

Initialize new persistent stores with a Quartz 4 schema. Tests using
`QuartzTimeAdjustment` should configure their scheduler with
`UseTimeProvider(new AdjustableTimeProvider())`.

## Packages and releases

- [Package identity](package-identity.md): the package ID map, version series, and package metadata.
- [Release process](releasing.md): versioning, package checks, approval, and publication.
- [Redistribution notes](legal/redistribution-inventory.md): license, notice, and third-party attribution requirements.

## Building and testing

- [CI coverage and local commands](ci.md): the workflows, the test matrix, coverage exclusions, and how to run suites locally.
- [Dependency inventory](architecture/dependency-inventory.md): the projects, references, and packages evaluated from MSBuild.
- [Scripts](../scripts/README.md): what each CI and release script does.
- [Legacy target audit](architecture/legacy-target-audit.md): the historical cleanup from multi-targeting to .NET 10.

## Project

- [Support](../SUPPORT.md), [security policy](../SECURITY.md), and [contribution guide](../CONTRIBUTING.md)
