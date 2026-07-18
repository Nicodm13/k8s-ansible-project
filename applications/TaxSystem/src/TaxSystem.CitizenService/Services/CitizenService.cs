using TaxSystem.CitizenService.Repositories;
using TaxSystem.Shared.Models;

namespace TaxSystem.CitizenService.Services;

public class CitizenService
{
    private readonly IWriteCitizenRepository _writeCitizenRepository;

    public CitizenService(IWriteCitizenRepository writeCitizenRepository)
    {
        _writeCitizenRepository = writeCitizenRepository;
    }

    public async Task RegisterCitizenAsync(Citizen citizen)
    {
        await _writeCitizenRepository.SaveAsync(citizen);
    }
}