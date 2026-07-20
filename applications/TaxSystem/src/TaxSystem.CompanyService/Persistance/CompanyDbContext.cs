using Microsoft.EntityFrameworkCore;
using TaxSystem.Shared.Models;

namespace TaxSystem.CompanyService.Persistance;

public class CompanyDbContext : DbContext
{
    public DbSet<Company> Companies => Set<Company>();

    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(c => c.CVR);
            entity.Property(c => c.CVR).HasMaxLength(20);
            entity.Property(c => c.Name).HasMaxLength(200);
        });
    }
}

