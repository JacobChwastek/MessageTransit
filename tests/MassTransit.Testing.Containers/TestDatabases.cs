namespace MassTransit.Testing.Containers;

using Microsoft.Data.SqlClient;
using Npgsql;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;


/// <summary>
/// SQL Server and PostgreSQL containers, using the administrator credentials the test suites already expect
/// </summary>
public static class TestDatabases
{
    public const string DefaultDatabase = "MassTransitUnitTests";
    public const string Password = "Password12!";

    public static readonly SharedContainer<MsSqlContainer> SqlServer = new(() => new MsSqlBuilder(ContainerImages.SqlServer)
        .WithPassword(Password)
        .Build());

    public static readonly SharedContainer<PostgreSqlContainer> PostgreSql = new(() => new PostgreSqlBuilder(ContainerImages.PostgreSql)
        .WithPassword(Password)
        .Build());

    public static string SqlServerConnectionString(string database = DefaultDatabase)
    {
        return new SqlConnectionStringBuilder(SqlServer.Instance.GetConnectionString()) { InitialCatalog = database }.ConnectionString;
    }

    public static string PostgresConnectionString(string database = DefaultDatabase)
    {
        return new NpgsqlConnectionStringBuilder(PostgreSql.Instance.GetConnectionString()) { Database = database }.ConnectionString;
    }
}
