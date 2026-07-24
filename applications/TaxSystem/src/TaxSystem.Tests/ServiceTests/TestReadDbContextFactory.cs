using Microsoft.EntityFrameworkCore;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.Tests.ServiceTests;

internal sealed class TestReadDbContextFactory<TContext> : IReadDbContextFactory<TContext>
    where TContext : DbContext
{
    private readonly DbContextOptions<TContext> _options;

    public TestReadDbContextFactory(DbContextOptions<TContext> options)
    {
        _options = options;
    }

    public TContext CreateDbContext()
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), _options)!;
    }
}
