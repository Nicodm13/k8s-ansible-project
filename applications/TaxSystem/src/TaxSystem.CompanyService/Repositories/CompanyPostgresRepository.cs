using Microsoft.EntityFrameworkCore;
using TaxSystem.CompanyService.Persistance;
using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.CompanyService.Repositories;

public class CompanyPostgresRepository : IReadCompanyRepository, IWriteCompanyRepository
{
    private readonly CompanyDbContext _writeDbContext;
    private readonly IReadDbContextFactory<CompanyDbContext> _readDbContextFactory;

    public CompanyPostgresRepository(
        CompanyDbContext writeDbContext,
        IReadDbContextFactory<CompanyDbContext> readDbContextFactory)
    {
        _writeDbContext = writeDbContext;
        _readDbContextFactory = readDbContextFactory;
    }

    public async Task<Company?> GetByCvrAsync(string cvr)
    {
        await using var readDbContext = _readDbContextFactory.CreateDbContext();
        return await readDbContext.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CVR == cvr);
    }

    public async Task SaveAsync(Company company)
    {
        var existing = await _writeDbContext.Companies.FindAsync(company.CVR);
        if (existing is null)
        {
            _writeDbContext.Companies.Add(company);
        }
        else
        {
            _writeDbContext.Entry(existing).CurrentValues.SetValues(company);
        }

        await _writeDbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string cvr)
    {
        var company = await _writeDbContext.Companies.FindAsync(cvr);
        if (company is null)
            return false;

        _writeDbContext.Companies.Remove(company);
        await _writeDbContext.SaveChangesAsync();
        return true;
    }
}

