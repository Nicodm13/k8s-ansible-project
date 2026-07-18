namespace TaxSystem.StatementGenerator.Repositories;

using TaxSystem.Shared.Models;

public interface IWriteStatementRepository
{
    Task SaveAsync(string cpr, Statement statement);
}
