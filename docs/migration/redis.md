# Upgrading the Redis saga repository to StackExchange.Redis 3

`MessageTransit.Redis` depends on StackExchange.Redis 3. An application that references StackExchange.Redis 2.x directly must move that reference to 3.x as well, because NuGet resolves one version per application and a lower direct reference is reported as a package downgrade.

The MessageTransit API is unchanged. `RedisRepository()`, `SetRedisSagaRepositoryProvider()`, `RedisSagaRepository<TSaga>.Create()`, `RedisConnectionFactory` and `SelectDatabase` keep their signatures. Saga documents, saga keys, lock keys and lock timeouts are stored as before, so sagas written with the previous version are read without migration, and both versions can run side by side during a rolling deployment.

## Changes that come from StackExchange.Redis 3

The [StackExchange.Redis releases](https://github.com/StackExchange/StackExchange.Redis/releases) describe every change. These are the ones an application may notice:

| Change in StackExchange.Redis 3 | Effect on sagas | What to do |
| --- | --- | --- |
| Connections use RESP3 by default on servers that support it. Servers that do not support RESP3 fall back to RESP2. | None. The saga repository uses typed string, key and lock commands, whose results are the same under both protocols. | Application code that reads raw results from `Execute`, `ScriptEvaluate` or Lua scripts can receive different shapes, for example maps instead of nested arrays. Test that code, or keep RESP2 with `protocol=resp2` in the connection string or `ConfigurationOptions.Protocol = RedisProtocol.Resp2`. |
| `Execute` and `ExecuteAsync` refuse admin commands unless `AllowAdmin` is set. | None. The saga repository does not send admin commands. | Set `allowAdmin=true` on connections that send commands such as `FLUSHALL` or `CLIENT KILL` through `Execute`. |
| `ConnectionMultiplexer.PreserveAsyncOrder` is obsolete as an error. | None. | Remove assignments to the property. Use `ChannelMessageQueue` when pub/sub messages must be processed in order. |
| The package depends on the new `RESPite` package. | None. | Allow the new package in any package allow list or offline feed. |

Other libraries in the application that reference StackExchange.Redis are resolved to 3.x as well. Check that each one supports StackExchange.Redis 3 before upgrading.

## Regression coverage

The [Redis integration tests](../../tests/MessageTransit.RedisIntegration.IntegrationTests) run against Redis and Valkey. They cover saga initiation and correlation with and without optimistic concurrency, key prefixes, container registration, the job service, RESP3 negotiation, exclusive lock ownership, lock and saga expiry, cancellation of a lock wait, concurrent updates to one saga in both concurrency modes, reconnecting after the server drops the connection, and closing connections on shutdown.
