using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class CompanyClientService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRequestClient<CompanyInfoRequested> _companyInfoClient;

    public CompanyClientService(
        IPublishEndpoint publishEndpoint,
        IRequestClient<CompanyInfoRequested> companyInfoClient)
    {
        _publishEndpoint = publishEndpoint;
        _companyInfoClient = companyInfoClient;
    }

    public async Task<Company?> getCompanyFromCvr(string cvr)
    {
        var response = await _companyInfoClient.GetResponse<CompanyInfoReceived, CompanyInfoNotFound>(
            new CompanyInfoRequested(cvr));

        if (response.Is(out Response<CompanyInfoReceived>? companyInfoReceived))
        {
            return new Company
            {
                CVR = companyInfoReceived.Message.Cvr,
                Name = companyInfoReceived.Message.Name
            };
        }

        return null;
    }

    public async Task SetEmployeeIncomeForYear(string cvr, int year, int cpr, int income)
    {
        await _publishEndpoint.Publish(new ReportSalary(cvr, year, cpr.ToString(), income));
    }

    public async Task RegisterCompany(Company company)
    {
        await _publishEndpoint.Publish(new CompanyRegistrationRequested(company.CVR, company.Name));
    }

    public async Task UpdateCompany(Company? company)
    {
        ArgumentNullException.ThrowIfNull(company);

        await _publishEndpoint.Publish(new CompanyUpdateRequested(company.CVR, company.Name));
    }
}
