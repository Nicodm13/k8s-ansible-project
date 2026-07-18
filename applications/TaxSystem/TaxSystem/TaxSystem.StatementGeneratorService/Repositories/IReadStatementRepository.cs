namespace TaxSystem.StatementGenerator.Repositories;

using TaxSystem.Shared.Models;

public interface IReadStatementRepository
{
    Task<Statement?> GetByCprAsync(string cpr);
}
