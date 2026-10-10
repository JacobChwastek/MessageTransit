namespace MassTransit.Azure.Cosmos.Tests;

using System;
using NUnit.Framework;


public static class Configuration
{
    public static string AccountEndpoint =>
        TestContext.Parameters.Exists("CosmosEndpoint")
            ? TestContext.Parameters.Get("CosmosEndpoint")
            : Environment.GetEnvironmentVariable("MT_COSMOS_ENDPOINT")
            ?? AzureCosmosEmulatorConstants.AccountEndpoint;

    public static string AccountKey =>
        TestContext.Parameters.Exists("CosmosKey")
            ? TestContext.Parameters.Get("CosmosKey")
            : Environment.GetEnvironmentVariable("MT_COSMOS_KEY")
            ?? AzureCosmosEmulatorConstants.AccountKey;

    public static string ConnectionString => $"AccountEndpoint={AccountEndpoint};AccountKey={AccountKey}";

    /// <summary>
    /// True unless an endpoint is supplied through a test parameter or MT_COSMOS_ENDPOINT
    /// </summary>
    public static bool UsesEmulator => !TestContext.Parameters.Exists("CosmosEndpoint") && Environment.GetEnvironmentVariable("MT_COSMOS_ENDPOINT") == null;
}
