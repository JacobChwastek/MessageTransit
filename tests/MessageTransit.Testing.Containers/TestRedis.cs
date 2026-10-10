namespace MessageTransit.Testing.Containers;

using System;
using Testcontainers.Redis;


/// <summary>
/// A Redis-compatible server. Set MT_REDIS_SERVER=valkey to run the same suite against Valkey.
/// </summary>
public static class TestRedis
{
    public static readonly SharedContainer<RedisContainer> Server = new(() => new RedisBuilder(Image).Build());

    /// <summary>
    /// The StackExchange.Redis configuration string for the running server
    /// </summary>
    public static string Configuration => Server.Instance.GetConnectionString();

    static string Image => string.Equals(Environment.GetEnvironmentVariable("MT_REDIS_SERVER"), "valkey", StringComparison.OrdinalIgnoreCase)
        ? ContainerImages.Valkey
        : ContainerImages.Redis;
}
