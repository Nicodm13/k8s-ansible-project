using Microsoft.EntityFrameworkCore;
using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;
using TaxSystem.StatementGenerator.Persistance;

namespace TaxSystem.StatementGenerator.Repositories;

public class StatementPostgresRepository : IReadStatementRepository, IWriteStatementRepository
{
    private readonly StatementDbContext _writeDbContext;
    private readonly IReadDbContextFactory<StatementDbContext> _readDbContextFactory;

    public StatementPostgresRepository(
        StatementDbContext writeDbContext,
        IReadDbContextFactory<StatementDbContext> readDbContextFactory)
    {
        _writeDbContext = writeDbContext;
        _readDbContextFactory = readDbContextFactory;
    }

    public async Task SaveReportAsync(string cpr, Statement statement)
    {
        statement.reportId ??= Guid.NewGuid().ToString("N");
        statement.cpr = cpr;
        _writeDbContext.Statements.Add(statement);
        await _writeDbContext.SaveChangesAsync();
    }

    public async Task<Statement?> GetMergedStatementAsync(string cpr)
    {
        await using var readDbContext = _readDbContextFactory.CreateDbContext();
        var reports = await readDbContext.Statements
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

