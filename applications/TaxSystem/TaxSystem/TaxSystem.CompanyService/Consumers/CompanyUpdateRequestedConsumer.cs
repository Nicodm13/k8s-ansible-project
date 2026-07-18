using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendCompanyService = TaxSystem.CompanyService.Services.CompanyService;

namespace TaxSystem.CompanyService.Consumers;

public class CompanyUpdateRequestedConsumer : IConsumer<CompanyUpdateRequested>
{
    private readonly BackendCompanyService _companyService;

    public CompanyUpdateRequestedConsumer(BackendCompanyService companyService)
    {
        _companyService = companyService;
    }

    public async Task Consume(ConsumeContext<CompanyUpdateRequested> context)
    {
        var company = await _companyService.UpdateCompanyAsync(context.Message.Cvr, context.Message.Name);
        await context.Publish(new CompanyUpdated(company.CVR, company.Name));
    }
}
