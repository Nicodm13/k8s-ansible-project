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

    public Task SaveReportAsync(string cpr, Statement statement)
    {
        statement.reportId ??= Guid.NewGuid().ToString("N");
        var key = $"{cpr}_{statement.reportId}";
        _repository.Save(key, statement);
        return Task.CompletedTask;
    }

    public Task<Statement?> GetMergedStatementAsync(string cpr)
    {
        var allStatements = _repository.GetAll<Statement>();

        var forCpr = allStatements
            .Where(s => string.Equals(s.cpr, cpr, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.reportedAt)
            .ToList();

        if (forCpr.Count == 0)
            return Task.FromResult<Statement?>(null);

        var merged = new Statement
        {
            cpr = cpr,
            reportId = forCpr[0].reportId,
            reportedAt = forCpr[0].reportedAt
        };

        foreach (var report in forCpr)
        {
            merged.name ??= report.name;
            merged.employerName ??= report.employerName;
            merged.annualGrossSalary ??= report.annualGrossSalary;
            merged.annualCapitalGains ??= report.annualCapitalGains;
            merged.annualTotalDeduction ??= report.annualTotalDeduction;
            merged.annualPaidTax ??= report.annualPaidTax;
            merged.annualTax ??= report.annualTax;
            merged.annualOwedTax ??= report.annualOwedTax;
        }

        return Task.FromResult<Statement?>(merged);
    }
}
