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
        var companyRegistered = new CompanyRegistered(company.CVR, company.Name);

        if (context.ResponseAddress is not null)
        {
            await context.RespondAsync(companyRegistered);
        }

        await context.Publish(companyRegistered);
    }
}
