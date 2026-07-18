using MassTransit;
using TaxSystem.Client.Services;
using TaxSystem.Shared.Messaging.Contracts;

namespace TaxSystem.Client.Consumers;

public class TaxInfoReportedConsumer : IConsumer<TaxInfoReported>
{
    private readonly TaxInfoService _taxInfoService;

    public TaxInfoReportedConsumer(TaxInfoService taxInfoService)
    {
        _taxInfoService = taxInfoService;
    }

    public Task Consume(ConsumeContext<TaxInfoReported> context)
    {
        _taxInfoService.RecordTaxInfo(context.Message);
        return Task.CompletedTask;
    }
}
