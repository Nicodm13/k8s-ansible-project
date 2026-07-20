using Microsoft.EntityFrameworkCore;
using TaxSystem.Shared.Models;

namespace TaxSystem.StatementGenerator.Persistance;

public class StatementDbContext : DbContext
{
    public DbSet<Statement> Statements => Set<Statement>();

    public StatementDbContext(DbContextOptions<StatementDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Statement>(entity =>
        {
            entity.ToTable("statements");
            entity.HasKey(s => s.reportId);
            entity.Property(s => s.reportId).HasMaxLength(50);
            entity.Property(s => s.cpr).HasMaxLength(20).IsRequired();
            entity.Property(s => s.reportedAt);
            entity.Property(s => s.name).HasMaxLength(200);
            entity.Property(s => s.employerName).HasMaxLength(200);
            entity.Property(s => s.annualGrossSalary).HasMaxLength(50);
            entity.Property(s => s.annualCapitalGains).HasMaxLength(50);
            entity.Property(s => s.annualTotalDeduction).HasMaxLength(50);
            entity.Property(s => s.annualPaidTax).HasMaxLength(50);
            entity.Property(s => s.annualTax).HasMaxLength(50);
            entity.Property(s => s.annualOwedTax).HasMaxLength(50);

            entity.HasIndex(s => s.cpr);
            entity.HasIndex(s => new { s.cpr, s.reportedAt });
        });
    }
}

