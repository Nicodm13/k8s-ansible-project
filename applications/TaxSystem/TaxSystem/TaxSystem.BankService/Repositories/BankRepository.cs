using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.BankService.Repositories;

public class BankRepository : IBankReadRepository, IBankWriteRepository
{
    private readonly FileSystemRepository _repository;

    public BankRepository(FileSystemRepository repository)
    {
        _repository = repository;
    }

    public Task<BankTransfer?> GetByCprAsync(string cpr)
    {
        return Task.FromResult(_repository.Get<BankTransfer>(cpr));
    }

    public Task SaveAsync(BankTransfer transfer)
    {
        _repository.Save(transfer.Cpr, transfer);
        return Task.CompletedTask;
    }
}
