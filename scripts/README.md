# Scripts

| Path | What it is |
| --- | --- |
| [`ci_build.py`](ci_build.py) | Builds every tracked project and checks that both benchmarks start. |
| [`ci_test_report.py`](ci_test_report.py) | Turns a `.trx` test result file into a GitHub Actions summary. |
| [`dependency_inventory.py`](dependency_inventory.py) | Writes the project and package inventory to `docs/architecture/`; `--check` fails when it is stale. |
| [`release_version.py`](release_version.py) | Calculates the next release version from release tags and Conventional Commits. |
| [`release_check.py`](release_check.py) | Inspects candidate packages and tests them in clean consumer projects. |
| [`release_publish.py`](release_publish.py) | Binds approved packages to a version and commit, then publishes them to NuGet.org. |
| [`release_smoke/`](release_smoke) | C# sources for the clean consumer projects used by `release_check.py`. |
| [`release_symbols/`](release_symbols) | .NET tool that checks Source Link and source checksums in packaged assemblies. |
| [`tests/`](tests) | Tests for the release scripts; see [tests/README.md](tests/README.md). |

The [release process](../docs/releasing.md) explains how the release scripts fit together.
