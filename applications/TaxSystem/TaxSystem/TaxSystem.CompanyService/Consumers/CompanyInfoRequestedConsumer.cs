using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendCompanyService = TaxSystem.CompanyService.Services.CompanyService;

namespace TaxSystem.CompanyService.Consumers;

public class CompanyInfoRequestedConsumer : IConsumer<CompanyInfoRequested>
{
    private readonly BackendCompanyService _companyService;

    public CompanyInfoRequestedConsumer(BackendCompanyService companyService)
    {
        _companyService = companyService;
    }

    public async Task Consume(ConsumeContext<CompanyInfoRequested> context)
    {
        var company = await _companyService.GetByCvrAsync(context.Message.Cvr);
        if (company is null)
        {
            await context.RespondAsync(new CompanyInfoNotFound(context.Message.Cvr));
            return;
        }

        await context.RespondAsync(new CompanyInfoReceived(company.CVR, company.Name));
    }
}
