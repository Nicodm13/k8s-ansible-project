using MassTransit;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class CitizenClientService
{
    private readonly TaxInfoService _taxInfoService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly EventAwaiter<CitizenRegistered> _citizenRegisteredAwaiter;

    private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(30);

    public CitizenClientService(
        TaxInfoService taxInfoService,
        IPublishEndpoint publishEndpoint,
        EventAwaiter<CitizenRegistered> citizenRegisteredAwaiter)
    {
        _taxInfoService = taxInfoService;
        _publishEndpoint = publishEndpoint;
        _citizenRegisteredAwaiter = citizenRegisteredAwaiter;
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
    /// <exception cref="TimeoutException">thrown if CitizenService does not confirm registration in time</exception>
    public async Task<Citizen> createCitizen(Citizen citizen)
    {
        _taxInfoService.RegisterCitizen(citizen);

        await _citizenRegisteredAwaiter.PublishAndWait(
            () => _publishEndpoint.Publish(new CitizenRegistrationRequested(
                citizen.cpr,
                citizen.firstName,
                citizen.lastName,
                citizen.streetAddress,
                citizen.city,
                citizen.zipCode,
                citizen.bankAccountNumber)),
            citizen.cpr,
            RegistrationTimeout);

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
