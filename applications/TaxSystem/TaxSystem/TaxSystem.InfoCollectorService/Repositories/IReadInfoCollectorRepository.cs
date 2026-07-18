namespace TaxSystem.InfoCollectorService.Repositories;

using TaxSystem.Shared.Models;

public interface IReadInfoCollectorRepository
{
    Task<TaxInfoRecord?> GetByCprAsync(string cpr);
}
