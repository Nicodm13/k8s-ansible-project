using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.StatementGenerator.Repositories;

public class StatementRepository : IReadStatementRepository, IWriteStatementRepository
{
    private readonly FileSystemRepository _repository;

    public StatementRepository(FileSystemRepository repository)
    {
        _repository = repository;
    }

    public Task<Statement?> GetByCprAsync(string cpr)
    {
        return Task.FromResult(_repository.Get<Statement>(cpr));
    }

    public Task SaveAsync(string cpr, Statement statement)
    {
        _repository.Save(cpr, statement);
        return Task.CompletedTask;
    }
}
