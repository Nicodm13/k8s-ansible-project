namespace TaxSystem.CompanyService.Repositories;

using TaxSystem.Shared.Models;

public interface IReadCompanyRepository
{
    Task<Company?> GetByCvrAsync(string cvr);
}
