using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class CompanyClientService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRequestClient<CompanyInfoRequested> _companyInfoClient;
    private readonly IRequestClient<CompanyRegistrationRequested> _companyRegistrationClient;
    private readonly IRequestClient<CompanyDeregistrationRequested> _companyDeregistrationClient;
    private readonly IRequestClient<ReportSalary> _reportSalaryClient;

    public CompanyClientService(
        IPublishEndpoint publishEndpoint,
        IRequestClient<CompanyInfoRequested> companyInfoClient,
        IRequestClient<CompanyRegistrationRequested> companyRegistrationClient,
        IRequestClient<CompanyDeregistrationRequested> companyDeregistrationClient,
        IRequestClient<ReportSalary> reportSalaryClient)
    {
        _publishEndpoint = publishEndpoint;
        _companyInfoClient = companyInfoClient;
        _companyRegistrationClient = companyRegistrationClient;
        _companyDeregistrationClient = companyDeregistrationClient;
        _reportSalaryClient = reportSalaryClient;
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

    public async Task<bool> SetEmployeeIncomeForYear(string cvr, int year, string cpr, int income, long? paidTax)
    {
        var response = await _reportSalaryClient.GetResponse<SalaryReported, CompanyInfoNotFound>(
            new ReportSalary(cvr, year, cpr, income, paidTax ?? 0));

        return response.Is(out Response<SalaryReported>? _);
    }

    public async Task RegisterCompany(Company company)
    {
        await _companyRegistrationClient.GetResponse<CompanyRegistered>(new CompanyRegistrationRequested(company.CVR, company.Name));
    }

    public async Task UpdateCompany(Company? company)
    {
        ArgumentNullException.ThrowIfNull(company);

        await _publishEndpoint.Publish(new CompanyUpdateRequested(company.CVR, company.Name));
    }

    public async Task DeregisterCompany(string cvr)
    {
        await _companyDeregistrationClient.GetResponse<CompanyDeregistered>(new CompanyDeregistrationRequested(cvr));
    }
}
