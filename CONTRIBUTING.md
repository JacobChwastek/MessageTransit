# Contributing to MessageTransit

MessageTransit accepts pull requests from forks of [JacobChwastek/MessageTransit](https://github.com/JacobChwastek/MessageTransit). GitHub Issues and Discussions are currently disabled; see [SUPPORT.md](SUPPORT.md) for contact availability. Reviews are best effort, and there is no guaranteed response time.

Do not describe a suspected vulnerability in a pull request or any other public place. Follow the [security policy](SECURITY.md) instead.

## License

MessageTransit is licensed under Apache-2.0. Contributions submitted for inclusion are provided under the same license, as described in section 5 of the [license](LICENSE). There is no contributor license agreement. Keep existing copyright notices, license text, and third-party attribution intact, and record new third-party material in [THIRD-PARTY-NOTICES](THIRD-PARTY-NOTICES).

## Prerequisites

- The .NET 10 SDK selected by [`global.json`](global.json).
- Python 3 for the CI build script.
- A running Docker engine for suites named `*.IntegrationTests`, which start their own containers through Testcontainers.

## Build and test

From the repository root:

```bash
python3 scripts/ci_build.py
```

This builds every project, including the three outside `MessageTransit.slnx`, and checks that both benchmark executables start.

Run a test project with the same options as CI:

```bash
dotnet test tests/MessageTransit.Tests -c Release -f net10.0 --filter 'Category!=Flaky'
```

Container-based suites need no further setup beyond Docker; a suite that uses fixed host ports conflicts with a local service already listening on them. [CI.md](CI.md) lists every test project, the services it uses, and which workflow runs it.

## Making a change

- Keep changes focused, and include tests that exercise the new or corrected behavior. A new suite that starts containers belongs in a project ending in `.IntegrationTests` and can use the shared setup in `tests/MessageTransit.Testing.Containers`.
- Mark a test `Category("Flaky")` only when its timing or environment cannot be made reliable; flaky tests are excluded from unattended runs.
- Match the style of the surrounding code. Use file-scoped namespaces in new or edited C# files where the file structure permits it, and keep base types and interfaces on the declaration line when it fits within 160 characters.
- Update the public documentation when behavior visible to consumers changes. Migration guides must stay usable on their own, without links to external project documentation.

## Commits and pull requests

- Use [Conventional Commits](https://www.conventionalcommits.org/) messages, for example `fix(rabbitmq): retry the channel after a broker restart`. Mark breaking changes with `!` and explain the migration in the commit body.
- Describe the change, the reason for it, and its effect on consumers. Include a minimal reproduction for bug fixes.
- List the checks you ran and their results, and say which relevant checks you did not run. The pull request template has a section for this.
- Pull requests target `develop`. Container-based integration suites run on pull requests into `master` and nightly on `develop`.
