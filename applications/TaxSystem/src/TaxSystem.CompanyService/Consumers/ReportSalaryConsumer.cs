using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendCompanyService = TaxSystem.CompanyService.Services.CompanyService;

namespace TaxSystem.CompanyService.Consumers;

public class ReportSalaryConsumer : IConsumer<ReportSalary>
{
    private readonly BackendCompanyService _companyService;

    public ReportSalaryConsumer(BackendCompanyService companyService)
    {
        _companyService = companyService;
    }

    public async Task Consume(ConsumeContext<ReportSalary> context)
    {
        var company = await _companyService.GetByCvrAsync(context.Message.Cvr);
        if (company is null)
        {
            return;
        }

        await context.Publish(new CompanyInfoReceived(company.CVR, company.Name));
        await context.Publish(new TaxInfoReported(
            context.Message.Cpr,
            string.Empty,
            context.Message.Income,
            0m,
            0m,
            0m));
    }
}
