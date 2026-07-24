using Microsoft.EntityFrameworkCore;
using TaxSystem.BankService.Persistance;
using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.BankService.Repositories;

public class BankPostgresRepository : IBankReadRepository, IBankWriteRepository
{
    private readonly BankDbContext _writeDbContext;
    private readonly IReadDbContextFactory<BankDbContext> _readDbContextFactory;

    public BankPostgresRepository(
        BankDbContext writeDbContext,
        IReadDbContextFactory<BankDbContext> readDbContextFactory)
    {
        _writeDbContext = writeDbContext;
        _readDbContextFactory = readDbContextFactory;
    }

    public async Task<BankTransfer?> GetByCprAsync(string cpr)
    {
        await using var readDbContext = _readDbContextFactory.CreateDbContext();
        var entity = await readDbContext.BankTransfers.AsNoTracking().FirstOrDefaultAsync(b => b.Cpr == cpr);
        return entity?.ToDomain();
    }

    public async Task SaveAsync(BankTransfer transfer)
    {
        var existing = await _writeDbContext.BankTransfers.FindAsync(transfer.Cpr);
        if (existing is null)
        {
            _writeDbContext.BankTransfers.Add(BankTransferEntity.FromDomain(transfer));
        }
        else
        {
            existing.Amount = transfer.Amount;
            existing.AccountNumber = transfer.AccountNumber;
            existing.RegistrationNumber = transfer.RegistrationNumber;
            existing.Status = transfer.Status;
        }

        await _writeDbContext.SaveChangesAsync();
    }
}

