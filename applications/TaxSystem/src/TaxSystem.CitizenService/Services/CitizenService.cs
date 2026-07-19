using TaxSystem.CitizenService.Repositories;
using TaxSystem.Shared.Models;

namespace TaxSystem.CitizenService.Services;

public class CitizenService
{
    private readonly IReadCitizenRepository _readCitizenRepository;
    private readonly IWriteCitizenRepository _writeCitizenRepository;

    public CitizenService(
        IReadCitizenRepository readCitizenRepository,
        IWriteCitizenRepository writeCitizenRepository)
    {
        _readCitizenRepository = readCitizenRepository;
        _writeCitizenRepository = writeCitizenRepository;
    }

    public Task<Citizen?> GetByCprAsync(string cpr)
    {
        return _readCitizenRepository.GetByCprAsync(cpr);
    }

    public async Task RegisterCitizenAsync(Citizen citizen)
    {
        await _writeCitizenRepository.SaveAsync(citizen);
    }

    public Task<bool> DeregisterCitizenAsync(string cpr)
    {
        return _writeCitizenRepository.DeleteAsync(cpr);
    }
}
