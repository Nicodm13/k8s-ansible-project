using Microsoft.EntityFrameworkCore;
using TaxSystem.CompanyService.Persistance;
using TaxSystem.Shared.Models;

namespace TaxSystem.CompanyService.Repositories;

public class CompanyPostgresRepository : IReadCompanyRepository, IWriteCompanyRepository
{
    private readonly CompanyDbContext _dbContext;

    public CompanyPostgresRepository(CompanyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Company?> GetByCvrAsync(string cvr)
    {
        return await _dbContext.Companies.FindAsync(cvr);
    }

    public async Task SaveAsync(Company company)
    {
        var existing = await _dbContext.Companies.FindAsync(company.CVR);
        if (existing is null)
        {
            _dbContext.Companies.Add(company);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(company);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string cvr)
    {
        var company = await _dbContext.Companies.FindAsync(cvr);
        if (company is null)
            return false;

        _dbContext.Companies.Remove(company);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}

