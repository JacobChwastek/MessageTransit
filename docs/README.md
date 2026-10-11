# MessageTransit documentation

Start with the repository [README](../README.md) for requirements, the package family, and a getting-started example.

## Migrating applications

- [Namespace and wire migration](migration/namespace-and-wire.md): moving a MassTransit 8.x application to MessageTransit, including renamed APIs, message identities, and stored data.
- [EF Core migration](migration/entity-framework-core.md): replacing the removed EF6 saga repository and audit store with EF Core.

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
