using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TaxSystem.Shared.Persistance;

public static class PostgresRegistration
{
    private const string ConnectionStringEnvVar = "TAXSYSTEM_DB_CONNECTION";

    /// <summary>
    /// Registers a DbContext backed by PostgreSQL, reading the connection string from the
    /// TAXSYSTEM_DB_CONNECTION environment variable. Falls back to the provided connection string
    /// if the environment variable is not set (useful for local development).
    /// </summary>
    public static IServiceCollection AddTaxSystemPostgres<TContext>(
        this IServiceCollection services,
        string? fallbackConnectionString = null)
        where TContext : DbContext
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar)
                               ?? fallbackConnectionString
                               ?? throw new InvalidOperationException(
                                   $"No database connection string configured. Set the {ConnectionStringEnvVar} environment variable.");

        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}

