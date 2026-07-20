using Microsoft.EntityFrameworkCore;
using TaxSystem.BankService.Persistance;
using TaxSystem.Shared.Models;

namespace TaxSystem.BankService.Repositories;

public class BankPostgresRepository : IBankReadRepository, IBankWriteRepository
{
    private readonly BankDbContext _dbContext;

    public BankPostgresRepository(BankDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BankTransfer?> GetByCprAsync(string cpr)
    {
        var entity = await _dbContext.BankTransfers.FindAsync(cpr);
        return entity?.ToDomain();
    }

    public async Task SaveAsync(BankTransfer transfer)
    {
        var existing = await _dbContext.BankTransfers.FindAsync(transfer.Cpr);
        if (existing is null)
        {
            _dbContext.BankTransfers.Add(BankTransferEntity.FromDomain(transfer));
        }
        else
        {
            existing.Amount = transfer.Amount;
            existing.AccountNumber = transfer.AccountNumber;
            existing.RegistrationNumber = transfer.RegistrationNumber;
            existing.Status = transfer.Status;
        }

        await _dbContext.SaveChangesAsync();
    }
}

