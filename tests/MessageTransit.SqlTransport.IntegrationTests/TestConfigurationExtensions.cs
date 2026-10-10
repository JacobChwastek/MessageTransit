namespace MessageTransit.DbTransport.Tests;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlTransport.PostgreSql;


public static class TestConfigurationExtensions
{
    public static IServiceCollection ConfigurePostgresTransport(this IServiceCollection services, bool create = true, bool delete = false)
    {
        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.Host = TestDatabases.PostgreSql.Hostname;
            options.Port = TestDatabases.PostgreSql.Port(5432);
            options.Database = "messagetransit_transport_tests";
            options.Schema = "transport";
            options.Role = "transport";
            options.Username = "unit_tests";
            options.Password = "H4rd2Gu3ss!";
            options.AdminUsername = "postgres";
            options.AdminPassword = TestDatabases.Password;
        });

        services.AddPostgresMigrationHostedService(create, delete);

        return services;
    }

    public static PostgresSqlTransportConnection GetTransportConnection(this IServiceProvider provider)
    {
        return PostgresSqlTransportConnection.GetDatabaseConnection(provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value);
    }
}
