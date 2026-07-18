namespace TaxSystem.InfoCollectorService.Repositories;

using TaxSystem.Shared.Models;

public interface IWriteInfoCollectorRepository
{
    Task SaveAsync(TaxInfoRecord taxInfo);
}
