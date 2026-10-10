namespace MessageTransit.EntityFrameworkCoreIntegration.Tests.Shared;

using Microsoft.EntityFrameworkCore;


/// <summary>
/// Configures a provider for specs that inspect the EF model or generated SQL and never open a connection
/// </summary>
public static class OfflineDbContextOptionsExtensions
{
    const string SqlServerConnectionString = "Server=unused;Database=unused;Integrated Security=True";
    const string PostgresConnectionString = "Host=unused;Database=unused";

    public static DbContextOptionsBuilder UseOfflineSqlServer(this DbContextOptionsBuilder builder)
    {
        return builder.UseSqlServer(SqlServerConnectionString);
    }

    public static DbContextOptionsBuilder<TContext> UseOfflineSqlServer<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
    {
        return builder.UseSqlServer(SqlServerConnectionString);
    }

    public static DbContextOptionsBuilder UseOfflineNpgsql(this DbContextOptionsBuilder builder)
    {
        return builder.UseNpgsql(PostgresConnectionString);
    }

    public static DbContextOptionsBuilder<TContext> UseOfflineNpgsql<TContext>(this DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
    {
        return builder.UseNpgsql(PostgresConnectionString);
    }
}
