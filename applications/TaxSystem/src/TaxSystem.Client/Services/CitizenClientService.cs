using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class CitizenClientService
{
    private readonly TaxInfoService _taxInfoService;
    private readonly IRequestClient<CitizenRegistrationRequested> _citizenRegistrationClient;

    public CitizenClientService(
        TaxInfoService taxInfoService,
        IRequestClient<CitizenRegistrationRequested> citizenRegistrationClient)
    {
        _taxInfoService = taxInfoService;
        _citizenRegistrationClient = citizenRegistrationClient;
    }

    public Statement GetStatementByCitizenIdAndYear(int citizenId, int year)
    {
        return _taxInfoService.GetStatementByCprAndYear(citizenId.ToString(), year);
    }
    

    public void ReportIncome(int citizenId, int year, int income)
    {
        _taxInfoService.setIncomeByCprAndYear(income, citizenId, year, "CitizenReport");
    }

    public void ReportDeductibles(int citizenId, int year, IEnumerable<Deductible> deductibles)
    {
        _taxInfoService.setDeductiblesByCprAndYear(deductibles, citizenId, year, "CitizenReport");
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="citizen"></param>
    /// <exception cref="InvalidOperationException">thrown if attempting to create a citizen that already exists</exception>
    /// <exception cref="RequestTimeoutException">thrown if CitizenService does not confirm registration in time</exception>
    public async Task<Citizen> createCitizen(Citizen citizen)
    {
        _taxInfoService.RegisterCitizen(citizen);

        await _citizenRegistrationClient.GetResponse<CitizenRegistered>(
            new CitizenRegistrationRequested(
                citizen.cpr,
                citizen.firstName,
                citizen.lastName,
                citizen.streetAddress,
                citizen.city,
                citizen.zipCode,
                citizen.bankAccountNumber));

        return citizen;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="citizen"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void updateCitizen(Citizen? citizen)
    {
        throw new NotImplementedException();
    }
}
