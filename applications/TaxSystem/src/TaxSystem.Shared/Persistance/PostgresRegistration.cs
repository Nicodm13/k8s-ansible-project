using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TaxSystem.Shared.Persistance;

public static class PostgresRegistration
{
    private const string WriteConnectionEnvVar = "TAXSYSTEM_DB_CONNECTION_WRITE";
    private const string ReadConnectionEnvVar = "TAXSYSTEM_DB_CONNECTION_READ";
    // Fallback for single-connection setups (local dev, backwards compat)
    private const string LegacyConnectionEnvVar = "TAXSYSTEM_DB_CONNECTION";

    /// <summary>
    /// Registers a DbContext with CQRS-aware read/write connection splitting.
    /// - Writes go to the primary (TAXSYSTEM_DB_CONNECTION_WRITE or fallback)
    /// - Reads are load-balanced across replicas (TAXSYSTEM_DB_CONNECTION_READ or fallback)
    ///
    /// In production with CloudNativePG:
    ///   WRITE → taxsystem-db-rw service (primary)
    ///   READ  → taxsystem-db-ro service (replicas, round-robin)
    ///
    /// For tests or local dev, a single fallback connection string can be provided.
    /// </summary>
    public static IServiceCollection AddTaxSystemPostgres<TContext>(
        this IServiceCollection services,
        string? fallbackConnectionString = null)
        where TContext : DbContext
    {
        var writeConnection = ResolveConnectionString(WriteConnectionEnvVar, fallbackConnectionString);
        var readConnection = ResolveConnectionString(ReadConnectionEnvVar, fallbackConnectionString);

        // Register the primary (write) DbContext as the default
        services.AddDbContext<TContext>(options =>
        {
            options.UseNpgsql(writeConnection, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });
        });

        // Register a named read-only DbContext options for CQRS read repositories
        services.AddSingleton(new ReadOnlyConnectionString(readConnection));
        services.AddSingleton<IReadDbContextFactory<TContext>, PostgresReadDbContextFactory<TContext>>();

        return services;
    }

    private static string ResolveConnectionString(string envVar, string? fallback)
    {
        return Environment.GetEnvironmentVariable(envVar)
               ?? Environment.GetEnvironmentVariable(LegacyConnectionEnvVar)
               ?? fallback
               ?? throw new InvalidOperationException(
                   $"No database connection string configured. Set {envVar} or {LegacyConnectionEnvVar} environment variable.");
    }
}

/// <summary>
/// Holds the read-only replica connection string for CQRS read repositories.
/// Injected as a singleton and used by read repository implementations to create
/// read-optimized DbContext instances that query replicas.
/// </summary>
public sealed record ReadOnlyConnectionString(string Value);

public interface IReadDbContextFactory<out TContext>
    where TContext : DbContext
{
    TContext CreateDbContext();
}

public sealed class PostgresReadDbContextFactory<TContext> : IReadDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly ReadOnlyConnectionString _connectionString;

    public PostgresReadDbContextFactory(ReadOnlyConnectionString connectionString)
    {
        _connectionString = connectionString;
    }

    public TContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(_connectionString.Value, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            })
            .Options;

        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }
}

