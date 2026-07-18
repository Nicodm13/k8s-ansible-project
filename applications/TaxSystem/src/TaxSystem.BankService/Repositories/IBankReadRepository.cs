namespace TaxSystem.BankService.Repositories;

using TaxSystem.Shared.Models;

public interface IBankReadRepository
{
    Task<BankTransfer?> GetByCprAsync(string cpr);
}
