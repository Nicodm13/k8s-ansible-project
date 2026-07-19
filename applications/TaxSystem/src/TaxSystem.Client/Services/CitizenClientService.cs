using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class CitizenClientService
{
    private readonly TaxInfoService _taxInfoService;
    private readonly IRequestClient<CitizenRegistrationRequested> _citizenRegistrationClient;
    private readonly IRequestClient<CitizenInfoRequested> _citizenInfoClient;
    private readonly IRequestClient<CitizenDeregistrationRequested> _citizenDeregistrationClient;

    public CitizenClientService(
        TaxInfoService taxInfoService,
        IRequestClient<CitizenRegistrationRequested> citizenRegistrationClient,
        IRequestClient<CitizenInfoRequested> citizenInfoClient,
        IRequestClient<CitizenDeregistrationRequested> citizenDeregistrationClient)
    {
        _taxInfoService = taxInfoService;
        _citizenRegistrationClient = citizenRegistrationClient;
        _citizenInfoClient = citizenInfoClient;
        _citizenDeregistrationClient = citizenDeregistrationClient;
    }

    public async Task<Citizen?> GetCitizenByCpr(string cpr)
    {
        var response = await _citizenInfoClient.GetResponse<CitizenInfoReceived, CitizenInfoNotFound>(
            new CitizenInfoRequested(cpr));

        if (response.Is(out Response<CitizenInfoReceived>? citizenInfoReceived))
        {
            return new Citizen
            {
                cpr = citizenInfoReceived.Message.Cpr,
                firstName = citizenInfoReceived.Message.FirstName,
                lastName = citizenInfoReceived.Message.LastName,
                streetAddress = citizenInfoReceived.Message.StreetAddress,
                city = citizenInfoReceived.Message.City,
                zipCode = citizenInfoReceived.Message.ZipCode,
                bankAccountNumber = citizenInfoReceived.Message.BankAccountNumber
            };
        }

        return null;
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
        await _citizenRegistrationClient.GetResponse<CitizenRegistered>(
            new CitizenRegistrationRequested(
                citizen.cpr,
                citizen.firstName,
                citizen.lastName,
                citizen.streetAddress,
                citizen.city,
                citizen.zipCode,
                citizen.bankAccountNumber));

        _taxInfoService.RegisterCitizen(citizen);

        return citizen;
    }

    public async Task DeregisterCitizen(string cpr)
    {
        await _citizenDeregistrationClient.GetResponse<CitizenDeregistered>(new CitizenDeregistrationRequested(cpr));
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
