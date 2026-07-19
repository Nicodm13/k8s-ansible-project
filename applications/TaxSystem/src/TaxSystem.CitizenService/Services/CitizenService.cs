namespace TaxSystem.CitizenService.Services;

using TaxSystem.CitizenService.Repositories;
using TaxSystem.Shared.Models;

public class CitizenService
{
    private readonly IReadCitizenRepository _readRepository;
    private readonly IWriteCitizenRepository _writeRepository;

    public CitizenService(
        IReadCitizenRepository readRepository,
        IWriteCitizenRepository writeRepository)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
    }

    public Task<Citizen?> GetByCprAsync(string cpr)
    {
        return _readRepository.GetByCprAsync(cpr);
    }

    public async Task<Citizen> RegisterCitizenAsync(Citizen citizen)
    {
        await _writeRepository.SaveAsync(citizen);
        return citizen;
    }
}
