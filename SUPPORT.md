# MessageTransit support

MessageTransit is independently maintained. Support availability and compatibility for this fork are described here and in the repository's migration guides.

## Current contact availability

Report a reproducible problem with the [bug report form](https://github.com/JacobChwastek/MessageTransit/issues/new?template=bug_report.yml). GitHub Discussions are disabled, and no commercial support service or community chat is offered.

The repository README, [package identity map](docs/package-identity.md), [MassTransit migration guide](docs/migration/namespace-and-wire.md), and [EF Core migration guide](docs/migration/entity-framework-core.md) explain the current source and migration requirements. For a suspected vulnerability, follow [SECURITY.md](SECURITY.md) instead of opening an issue.

## Prepare a reproducible report

Include these details in a bug report or a [contribution](CONTRIBUTING.md):

- The exact MessageTransit package versions, or the source commit and branch for a local build.
- The .NET SDK and runtime versions, operating system, and affected transport or persistence provider.
- Broker or database versions and the relevant configuration, with credentials removed.
- A small project or code sample, the steps to run it, and the expected and actual behavior.
- Relevant exception details and logs covering application startup and the failing operation.

If the problem involves migration, identify whether queues, message envelopes, or stored saga state originated from an earlier system. MessageTransit has its own assembly, namespace, and wire identities; the migration guide describes the compatibility boundaries.

## Collect application logs

MessageTransit uses `Microsoft.Extensions.Logging`. Configure a logging provider in the host application, and temporarily set the `MessageTransit` category to `Debug` while reproducing the problem. In applications that load standard .NET logging settings from `appsettings.json`, merge this section into the application's existing configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MessageTransit": "Debug"
    }
  }
}
```

The application must have a provider configured to write those logs, such as console or a file provider. Capture startup through the failing operation, then restore the usual log level. Review logs and the reproduction for credentials, connection strings, personal data, and message contents before sharing them.
