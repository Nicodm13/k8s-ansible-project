using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.CitizenService.Repositories;

public class CitizenRepository : IReadCitizenRepository, IWriteCitizenRepository
{
    private readonly FileSystemRepository _repository;

    public CitizenRepository(FileSystemRepository repository)
    {
        _repository = repository;
    }

    public Task<Citizen?> GetByCprAsync(string cpr)
    {
        return Task.FromResult(_repository.Get<Citizen>(cpr));
    }

    public Task SaveAsync(Citizen citizen)
    {
        _repository.Save(citizen.cpr, citizen);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string cpr)
    {
        return Task.FromResult(_repository.Delete(cpr));
    }
}
