using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.InfoCollectorService.Repositories;

public class InfoCollectorRepository : IReadInfoCollectorRepository, IWriteInfoCollectorRepository
{
    private readonly FileSystemRepository _repository;

    public InfoCollectorRepository(FileSystemRepository repository)
    {
        _repository = repository;
    }

    public Task<TaxInfoRecord?> GetByCprAsync(string cpr)
    {
        return Task.FromResult(_repository.Get<TaxInfoRecord>(cpr));
    }

    public Task SaveAsync(TaxInfoRecord taxInfo)
    {
        _repository.Save(taxInfo.Cpr, taxInfo);
        return Task.CompletedTask;
    }
}
