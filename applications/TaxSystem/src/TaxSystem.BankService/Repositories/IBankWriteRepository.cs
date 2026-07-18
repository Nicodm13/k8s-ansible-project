namespace TaxSystem.BankService.Repositories;

using TaxSystem.Shared.Models;

public interface IBankWriteRepository
{
    Task SaveAsync(BankTransfer transfer);
}
