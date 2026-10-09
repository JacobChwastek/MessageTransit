namespace MassTransit.EntityFrameworkCoreIntegration.Tests;

using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Npgsql;
using NUnit.Framework;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;


/// <summary>
/// Starts the SQL Server and PostgreSQL containers on first use and removes them after the test run
/// </summary>
[SetUpFixture]
public class TestDatabases
{
    public const string DefaultDatabase = "MassTransitUnitTests";

    static readonly Lazy<Task<MsSqlContainer>> SqlServer = new(() => Start(new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04").Build()));
    static readonly Lazy<Task<PostgreSqlContainer>> Postgres = new(() => Start(new PostgreSqlBuilder("postgres:17.10").Build()));

    public static string SqlServerConnectionString(string database = DefaultDatabase)
    {
        var container = SqlServer.Value.GetAwaiter().GetResult();

        return new SqlConnectionStringBuilder(container.GetConnectionString()) { InitialCatalog = database }.ConnectionString;
    }

    public static string PostgresConnectionString(string database = DefaultDatabase)
    {
        var container = Postgres.Value.GetAwaiter().GetResult();

        return new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = database }.ConnectionString;
    }

    [OneTimeTearDown]
    public async Task Remove_containers()
    {
        // a container that failed to start has already failed the tests that needed it
        if (SqlServer.IsValueCreated && SqlServer.Value.IsCompletedSuccessfully)
            await SqlServer.Value.Result.DisposeAsync();

        if (Postgres.IsValueCreated && Postgres.Value.IsCompletedSuccessfully)
            await Postgres.Value.Result.DisposeAsync();
    }

    static async Task<TContainer> Start<TContainer>(TContainer container)
        where TContainer : DotNet.Testcontainers.Containers.IContainer
    {
        await container.StartAsync();

        return container;
    }
}
