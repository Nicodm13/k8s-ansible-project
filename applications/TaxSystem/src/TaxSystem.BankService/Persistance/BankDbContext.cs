using Microsoft.EntityFrameworkCore;
using TaxSystem.Shared.Models;

namespace TaxSystem.BankService.Persistance;

public class BankDbContext : DbContext
{
    public DbSet<BankTransferEntity> BankTransfers => Set<BankTransferEntity>();

    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BankTransferEntity>(entity =>
        {
            entity.ToTable("bank_transfers");
            entity.HasKey(b => b.Cpr);
            entity.Property(b => b.Cpr).HasMaxLength(20);
            entity.Property(b => b.Amount).HasPrecision(18, 2);
            entity.Property(b => b.AccountNumber).HasMaxLength(50);
            entity.Property(b => b.RegistrationNumber).HasMaxLength(20);
            entity.Property(b => b.Status).HasMaxLength(50);
        });
    }
}

/// <summary>
/// EF Core entity for bank transfers. The domain model is a record (immutable),
/// so we use a mutable entity class for persistence and map to/from the domain record.
/// </summary>
public class BankTransferEntity
{
    public string Cpr { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public BankTransfer ToDomain() => new(Cpr, Amount, AccountNumber, RegistrationNumber, Status);

    public static BankTransferEntity FromDomain(BankTransfer transfer) => new()
    {
        Cpr = transfer.Cpr,
        Amount = transfer.Amount,
        AccountNumber = transfer.AccountNumber,
        RegistrationNumber = transfer.RegistrationNumber,
        Status = transfer.Status
    };
}

