using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendCompanyService = TaxSystem.CompanyService.Services.CompanyService;

namespace TaxSystem.CompanyService.Consumers;

public class CompanyDeregistrationRequestedConsumer : IConsumer<CompanyDeregistrationRequested>
{
    private readonly BackendCompanyService _companyService;

    public CompanyDeregistrationRequestedConsumer(BackendCompanyService companyService)
    {
        _companyService = companyService;
    }

    public async Task Consume(ConsumeContext<CompanyDeregistrationRequested> context)
    {
        await _companyService.DeregisterCompanyAsync(context.Message.Cvr);
        await context.Publish(new CompanyDeregistered(context.Message.Cvr));
    }
}
