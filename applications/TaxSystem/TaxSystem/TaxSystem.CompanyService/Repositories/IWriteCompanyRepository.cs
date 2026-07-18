namespace TaxSystem.CompanyService.Repositories;

using TaxSystem.Shared.Models;

public interface IWriteCompanyRepository
{
    Task SaveAsync(Company company);
    Task<bool> DeleteAsync(string cvr);
}
