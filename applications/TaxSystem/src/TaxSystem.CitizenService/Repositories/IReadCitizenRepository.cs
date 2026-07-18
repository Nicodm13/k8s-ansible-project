namespace TaxSystem.CitizenService.Repositories;

using TaxSystem.Shared.Models;

public interface IReadCitizenRepository
{
    Task<Citizen?> GetByCprAsync(string cpr);
}
