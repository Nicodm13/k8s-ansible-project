namespace TaxSystem.CitizenService.Repositories;

using TaxSystem.Shared.Models;

public interface IWriteCitizenRepository
{
    Task SaveAsync(Citizen citizen);
    Task<bool> DeleteAsync(string cpr);
}
