using Microsoft.EntityFrameworkCore;
using TaxSystem.CitizenService.Persistance;
using TaxSystem.Shared.Models;

namespace TaxSystem.CitizenService.Repositories;

public class CitizenPostgresRepository : IReadCitizenRepository, IWriteCitizenRepository
{
    private readonly CitizenDbContext _dbContext;

    public CitizenPostgresRepository(CitizenDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Citizen?> GetByCprAsync(string cpr)
    {
        return await _dbContext.Citizens.FindAsync(cpr);
    }

    public async Task SaveAsync(Citizen citizen)
    {
        var existing = await _dbContext.Citizens.FindAsync(citizen.cpr);
        if (existing is null)
        {
            _dbContext.Citizens.Add(citizen);
        }
        else
        {
            _dbContext.Entry(existing).CurrentValues.SetValues(citizen);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string cpr)
    {
        var citizen = await _dbContext.Citizens.FindAsync(cpr);
        if (citizen is null)
            return false;

        _dbContext.Citizens.Remove(citizen);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}

