# Upgrading the Marten saga repository to Marten 9

`MessageTransit.Marten` depends on Marten 9. Marten 7.0.0 through 9.12.0 are affected by [GHSA-rfx3-98h7-v3xp](https://github.com/advisories/GHSA-rfx3-98h7-v3xp), a SQL injection in Marten's LINQ provider: a string such as a dictionary indexer key is written into the generated SQL without escaping. No patched Marten 8 release exists. The saga repository passes saga query correlations to Marten LINQ, so a correlation that indexes a dictionary with a value taken from a message, for example `x => x.Attributes[message.Key] == value`, can reach the vulnerable code on those versions.

The MessageTransit API is unchanged. `MartenRepository()`, `SetMartenSagaRepositoryProvider()` and `MartenSagaRepository<TSaga>.Create()` keep their signatures, and the repository still maps `CorrelationId` as the document identity with optimistic concurrency.

## Changes that come from Marten 9

Marten's [migration guide](https://martendb.io/migration-guide.html#key-changes-in-9-0-0) covers every change. These are the ones that affect saga storage:

| Change in Marten 9 | Effect on sagas | What to do |
| --- | --- | --- |
| System.Text.Json replaces Newtonsoft.Json as the default serializer. | Saga documents written by Marten 8 are read with System.Text.Json. Newtonsoft attributes, converters and settings no longer apply, and a property without a public setter is populated only through a constructor parameter or `[JsonInclude]`. | Load existing saga documents in a test before switching. To keep the previous format, reference `Marten.Newtonsoft` and call `options.UseNewtonsoftForSerialization()` in `AddMarten`. |
| `[Identity]` moved from `Marten.Schema` to `JasperFx`. | Saga classes that use the attribute no longer compile. | Replace `using Marten.Schema;` with `using JasperFx;`. Sagas registered with `MartenRepository()` do not need the attribute. |
| Document mappings are built when a session first uses them. | A mapping error in a saga type is reported by the first message for that saga, not at startup. | Call `store.Storage.BuildAllMappings()` at startup, or cover each saga type in an integration test. |
| Marten migrates some schema objects, including numeric revision columns and event store functions. | Saga tables mapped by MessageTransit use Guid versions for optimistic concurrency and are not affected by the `mt_version` change to `bigint`. | Apply Marten's schema migration as usual: `ApplyAllDatabaseChangesOnStartup()`, or a generated patch reviewed against a copy of the database. |
| An injected `IDocumentSession` is a lightweight session by default. | The saga repository opens its own dirty-tracked sessions and is not affected. | Only application code that relies on an identity map needs `.UseIdentitySessions()`. |
| Synchronous data access and .NET 8 support are removed; JasperFx moves to 2.x and Weasel to 9.x. | The repository uses asynchronous calls only and targets .NET 10. | Upgrade any direct `JasperFx` or `Weasel` references together with Marten. |

Marten 9 requires Npgsql 9.0.4 or later. An application that also references `MessageTransit.SqlTransport.PostgreSql` or the Npgsql EF Core provider resolves Npgsql 10.

## Regression coverage

The [Marten integration tests](../../tests/MessageTransit.MartenIntegration.Tests) run against PostgreSQL and cover saga initiation, correlation by ID and by query, observed and concurrent updates, container registration, multiple saga types, rejection of a stale update, discarding changes when a saga faults, and a dictionary key containing a quote that must not match other sagas.
