using Microsoft.EntityFrameworkCore;
using TaxSystem.Shared.Models;

namespace TaxSystem.CitizenService.Persistance;

public class CitizenDbContext : DbContext
{
    public DbSet<Citizen> Citizens => Set<Citizen>();

    public CitizenDbContext(DbContextOptions<CitizenDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Citizen>(entity =>
        {
            entity.ToTable("citizens");
            entity.HasKey(c => c.cpr);
            entity.Property(c => c.cpr).HasMaxLength(20);
            entity.Property(c => c.firstName).HasMaxLength(100).IsRequired(false);
            entity.Property(c => c.lastName).HasMaxLength(100).IsRequired(false);
            entity.Property(c => c.streetAddress).HasMaxLength(200).IsRequired(false);
            entity.Property(c => c.city).HasMaxLength(100).IsRequired(false);
            entity.Property(c => c.zipCode).HasMaxLength(10).IsRequired(false);
            entity.Property(c => c.bankAccountNumber).HasMaxLength(50).IsRequired(false);
        });
    }
}


