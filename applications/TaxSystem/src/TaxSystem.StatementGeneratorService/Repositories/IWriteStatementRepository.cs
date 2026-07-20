namespace TaxSystem.StatementGenerator.Repositories;

using TaxSystem.Shared.Models;

public interface IWriteStatementRepository
{
    Task SaveReportAsync(string cpr, Statement statement);
}
