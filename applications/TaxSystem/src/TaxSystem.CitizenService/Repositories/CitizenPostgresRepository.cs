using Microsoft.EntityFrameworkCore;
using TaxSystem.CitizenService.Persistance;
using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.CitizenService.Repositories;

public class CitizenPostgresRepository : IReadCitizenRepository, IWriteCitizenRepository
{
    private readonly CitizenDbContext _writeDbContext;
    private readonly IReadDbContextFactory<CitizenDbContext> _readDbContextFactory;

    public CitizenPostgresRepository(
        CitizenDbContext writeDbContext,
        IReadDbContextFactory<CitizenDbContext> readDbContextFactory)
    {
        _writeDbContext = writeDbContext;
        _readDbContextFactory = readDbContextFactory;
    }

    public async Task<Citizen?> GetByCprAsync(string cpr)
    {
        await using var readDbContext = _readDbContextFactory.CreateDbContext();
        return await readDbContext.Citizens.AsNoTracking().FirstOrDefaultAsync(c => c.cpr == cpr);
    }

    public async Task SaveAsync(Citizen citizen)
    {
        var existing = await _writeDbContext.Citizens.FindAsync(citizen.cpr);
        if (existing is null)
        {
            _writeDbContext.Citizens.Add(citizen);
        }
        else
        {
            _writeDbContext.Entry(existing).CurrentValues.SetValues(citizen);
        }

        await _writeDbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(string cpr)
    {
        var citizen = await _writeDbContext.Citizens.FindAsync(cpr);
        if (citizen is null)
            return false;

        _writeDbContext.Citizens.Remove(citizen);
        await _writeDbContext.SaveChangesAsync();
        return true;
    }
}

