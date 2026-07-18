using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class CitizenService
{
    private readonly TaxInfoService _taxInfoService;
    private readonly IPublishEndpoint _publishEndpoint;

    public CitizenService(TaxInfoService taxInfoService, IPublishEndpoint publishEndpoint)
    {
        _taxInfoService = taxInfoService;
        _publishEndpoint = publishEndpoint;
    }

    public Statement GetStatementByCitizenIdAndYear(int citizenId, int year)
    {
        throw new NotImplementedException();
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
    public async Task<Citizen> createCitizen(Citizen citizen)
    {
        await _publishEndpoint.Publish(new CitizenRegistrationRequested(
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


