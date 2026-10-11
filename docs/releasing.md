# Releasing MessageTransit

The [release workflow](../.github/workflows/release.yml) builds and inspects candidate packages from `master`. Publishing requires successful build, unit and integration checks for the candidate commit, followed by approval in the protected `nuget-org` environment. Development merges target `develop`; a separate pull request promotes release-ready changes to `master`. Ordinary branch and pull request builds do not publish packages.

## Versions

All packages in a release share one [semantic version](https://semver.org/). The first public candidate is **`1.0.0-preview.1`**. Preview is the default release channel; a stable release requires an explicit selection. Builds without an explicit version produce `1.0.0-dev` packages for local use.

The release calculator uses the fork's `messagetransit-v<version>` tags, including versions reserved by unfinished publications. Upstream `v*` tags do not affect fork versions. Conventional Commits merged since the latest stable release set the minimum base version: a breaking change (`!` or a `BREAKING CHANGE:` footer) requires a major increment, `feat:` a minor one, and anything else a patch. With the default `auto` bump, a preview series advances its preview number while its base version covers those changes, and otherwise starts a new series at the required base version. Selecting `stable` promotes the series to its base version. An explicit `bump` starts a new base version. The optional `version` input selects an exact higher version and must match the chosen channel. The newest `Release-As:` footer selects a base version on either channel, for example `Release-As: 2.0.0` produces `2.0.0-preview.1` on the preview channel; a footer that is not higher than the latest release is ignored with a warning. Run the calculator locally to inspect the next candidate:

```sh
python3 scripts/release_version.py --channel preview
```

Selecting the stable channel or supplying an explicit version is a release decision, and still requires all validation and publication approvals. NuGet package versions are permanent: an uploaded version cannot be replaced. A reserved version belongs to one approved candidate even if publication fails partway through.

## Package set and checks

[`release/packages.json`](../release/packages.json) separates packages selected for publication from packages held back with a reason. Every packable source project must appear in that manifest. Publishing a package that depends on a held package fails validation. Move a held package into the publish list only after its required source and package-consumer checks are available and passing.

Candidate validation includes:

- package IDs, coordinated versions, target frameworks, dependency declarations, README, license and notice files;
- expected managed assembly assets and identities;
- embedded SourceLink mappings and source document checksums for the candidate checkout;
- a clean .NET 10 consumer that checks the complete package set can be installed together;
- minimally referenced consumers that exercise core messaging, both alternative serializers, EF Core persistence, and RabbitMQ against a broker started with Docker, so direct references do not hide missing transitive dependencies;
- the full source build and test matrix described in the [CI coverage guide](ci.md), including both Linux and Windows build/unit lanes and the provisioned service suites.

Package-only consumers and tests built with project references provide complementary evidence. The inspection report identifies the scenarios actually executed. A successful build of an integration is not evidence that its runtime scenario passed.

## One-time setup

1. **NuGet.org ownership.** Confirm that the publishing account can publish every selected package ID. Consider an [ID prefix reservation](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation) covering both `MessageTransit` and `MessageTransit.*`.
2. **Trusted publishing.** Configure a [NuGet trusted publishing policy](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) for repository owner `JacobChwastek`, repository `MessageTransit`, workflow file `release.yml`, and environment `nuget-org`. Allow the required new-package and new-version operations for the selected package IDs.
3. **Repository variable.** Set `NUGET_USER` to the NuGet.org user name expected by that policy.
4. **Protected environment.** Create `nuget-org` with a required maintainer reviewer and restrict deployment branches to `master`. A separate job checks that an approval rule exists; until it does, candidates are still built and inspected but cannot be published. OIDC credentials are requested only after approval.
5. **Tag permissions.** Allow the publishing workflow to create the fork's release tags and GitHub releases. Those tags must not be moved or deleted to recycle a package version.

Before approving the first public release:

- Enable GitHub private vulnerability reporting and update [SECURITY.md](../SECURITY.md).
- Provide a public bug-reporting route and update [SUPPORT.md](../SUPPORT.md).
- Resolve the public identity checks described in the [package identity guide](package-identity.md#public-facing-metadata-and-artwork).

## Candidate review and publication

1. Merge the intended changes into `master` through a pull request, or run a manual dry run to inspect a candidate first.
2. The workflow calculates a version, builds and packs the libraries, inspects the packages and runs the package consumers. The full source build and integration workflows must pass for the same commit.
3. Review the retained `release-candidate` artifact and workflow summaries. The artifact contains the packages, inspection report, `approved.txt` and `candidate.json`. The candidate manifest binds the version and source commit to the approved package filenames, archive hashes and payload hashes.
4. Approve the `nuget-org` deployment for that exact candidate. The publisher checks the files again and reserves the version with an annotated tag before uploading the first package.
5. The publisher uploads only approved files. An existing NuGet version is accepted only when its package payload matches the reserved candidate; when NuGet.org reports an upload that it is still validating, the publisher waits up to 20 minutes for it to become downloadable before comparing. The comparison excludes NuGet's repository-signature entry, which can be added by the feed; it does not ignore differences in assemblies, metadata or notices.
6. After all selected packages are accounted for, the workflow records the GitHub release with its artifacts, the inspection report, each package's NuGet.org URL, the publishing account and the workflow run. Confirm that each selected package is available on NuGet.org and owned by the expected account.

## Recovering an interrupted publication

Use the workflow's `candidate_run` input to resume the retained artifact from the original workflow run. Run recovery from `master` and review that same candidate again. Recovery validates the artifact, checks the reservation, and runs the required source checks at the candidate's commit. It must not rebuild different package files under an already reserved version.

A new candidate normally has to match the latest `master` commit before its version is reserved. An existing reservation can resume its original commit after `master` advances, provided its manifest and package files still match. A newer candidate uses a new version.

If an existing package has different contents, publication stops. Do not bypass that failure with `--skip-duplicate`, move the tag, or substitute new files. Investigate the discrepancy and use a new version for changed contents. If the retained artifact has expired, restore the exact original artifact from a verified backup or abandon that version; rebuilding is not a substitute for the approved files.

## Release record

The workflow attaches the candidate manifest, inspection report and published package files to the GitHub release. The record should include:

- the version, source commit and candidate workflow run;
- the NuGet.org URL and owning account for each published package;
- the source CI results and package-consumer scenarios that passed;
- the supported integrations and known exclusions, including every held package.

A local build or inspection of an uncommitted checkout is development evidence. It is not a reproducible public release candidate for the recorded Git commit. Final publication requires the clean committed checkout used by the release workflow.
