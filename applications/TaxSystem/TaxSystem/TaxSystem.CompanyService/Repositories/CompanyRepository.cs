using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.CompanyService.Repositories;

public class CompanyRepository : IReadCompanyRepository, IWriteCompanyRepository
{
    private readonly FileSystemRepository _repository;

    public CompanyRepository(FileSystemRepository repository)
    {
        _repository = repository;
    }

    public Task<Company?> GetByCvrAsync(string cvr)
    {
        return Task.FromResult(_repository.Get<Company>(cvr));
    }

    public Task SaveAsync(Company company)
    {
        _repository.Save(company.CVR, company);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string cvr)
    {
        return Task.FromResult(_repository.Delete(cvr));
    }
}
