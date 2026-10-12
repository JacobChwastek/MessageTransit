# Release script tests

```bash
python3 -B -m unittest discover -s scripts/tests
```

| File | What it tests |
| --- | --- |
| [`test_release_version.py`](test_release_version.py) | Version calculation in `release_version.py`. |
| [`test_release_check.py`](test_release_check.py) | Package inspection and consumer checks in `release_check.py`. |
| [`test_release_publish.py`](test_release_publish.py) | Candidate manifests, version reservation and publishing in `release_publish.py`. |
| [`test_release_symbols.py`](test_release_symbols.py) | The `release_symbols` tool, using small assemblies compiled during the test; needs the .NET 10 SDK. |
| [`test_dependency_inventory.py`](test_dependency_inventory.py) | Staleness checks in `dependency_inventory.py`. |
| [`test_release_workflows.py`](test_release_workflows.py) | Required settings in the release, build and integration workflows. |

The tests run offline and in CI on every build. Output lines such as `Reserved messagetransit-v…` are progress messages, not errors.
