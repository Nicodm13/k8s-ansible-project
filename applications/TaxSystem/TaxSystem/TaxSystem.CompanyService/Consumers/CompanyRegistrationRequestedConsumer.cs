using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendCompanyService = TaxSystem.CompanyService.Services.CompanyService;

namespace TaxSystem.CompanyService.Consumers;

public class CompanyRegistrationRequestedConsumer : IConsumer<CompanyRegistrationRequested>
{
    private readonly BackendCompanyService _companyService;

    public CompanyRegistrationRequestedConsumer(BackendCompanyService companyService)
    {
        _companyService = companyService;
    }

    public async Task Consume(ConsumeContext<CompanyRegistrationRequested> context)
    {
        var company = await _companyService.RegisterCompanyAsync(context.Message.Cvr, context.Message.Name);
        await context.Publish(new CompanyRegistered(company.CVR, company.Name));
    }
}
