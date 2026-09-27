# Fork provenance and initial support policy

## Source and license

MessageTransit is an independently maintained derivative of [MassTransit/MassTransit](https://github.com/MassTransit/MassTransit), based on its `v8.5.10` tag at commit [`62ab339afa3bac2e9b3fe1769d0d35d7e44778e9`](https://github.com/MassTransit/MassTransit/commit/62ab339afa3bac2e9b3fe1769d0d35d7e44778e9). The upstream Git history and attribution are retained. MessageTransit is not an official MassTransit release, and its support is provided by the MessageTransit maintainer.

The inherited source includes an Apache-2.0 `LICENSE` and upstream `NOTICE`. The license and applicable notices must be preserved in source and distributed artifacts. A separate review of third-party assets, changed-file notices, and package contents is required before publishing packages. The Apache-licensed v8 source does not grant rights to the separate commercial MassTransit v9 line or to upstream trademarks. Candidate upstream fixes will be reviewed individually for provenance, applicable terms, and compatibility before adoption.

## Planned releases and compatibility

The repository is public, and the first intended NuGet packages are public **prereleases** for **.NET 10** applications. No release is designated as supported by this decision record. .NET Framework and older .NET application runtimes are outside the planned support scope.

All existing integrations remain in source initially. A release will identify the packages, transports, persistence providers, and versions actually verified; source inclusion alone is not a support claim. EF6 remains a transition case while migration to EF Core is planned.

The planned `MassTransit` to `MessageTransit` namespace, assembly, and package rename is intentionally breaking. Message contracts, broker topology, and persisted data may also require migration. No drop-in source, binary, or wire compatibility with MassTransit is promised. Prerelease APIs and package layout may change.

## Maintenance and reporting

The MessageTransit maintainers own release and support decisions. Public contributions may be reviewed under this repository's Apache-2.0 license; there is no contributor license agreement planned at this stage. Support is best effort, without a guaranteed response or fix time or a commercial support contract.

Before the first public NuGet prerelease, the project must provide a public bug-reporting route and enable the private vulnerability-reporting route described in [SECURITY.md](../../SECURITY.md). Each release must state its verified support scope and known exclusions.
