using Microsoft.EntityFrameworkCore;
using TaxSystem.Shared.Models;
using TaxSystem.StatementGenerator.Persistance;

namespace TaxSystem.StatementGenerator.Repositories;

public class StatementPostgresRepository : IReadStatementRepository, IWriteStatementRepository
{
    private readonly StatementDbContext _dbContext;

    public StatementPostgresRepository(StatementDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveReportAsync(string cpr, Statement statement)
    {
        statement.reportId ??= Guid.NewGuid().ToString("N");
        statement.cpr = cpr;
        _dbContext.Statements.Add(statement);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Statement?> GetMergedStatementAsync(string cpr)
    {
        var reports = await _dbContext.Statements
            .Where(s => s.cpr == cpr)
            .OrderByDescending(s => s.reportedAt)
            .AsNoTracking()
            .ToListAsync();

        if (reports.Count == 0)
            return null;

        var merged = new Statement
        {
            cpr = cpr,
            reportId = reports[0].reportId,
            reportedAt = reports[0].reportedAt
        };

        foreach (var report in reports)
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

        return merged;
    }
}

