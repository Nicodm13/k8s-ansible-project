using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendStatementGeneratorService = TaxSystem.StatementGenerator.Services.StatementGeneratorService;

namespace TaxSystem.StatementGenerator.Consumers;

public class TaxInfoReportedConsumer : IConsumer<TaxInfoReported>
{
    private readonly BackendStatementGeneratorService _statementGeneratorService;

    public TaxInfoReportedConsumer(BackendStatementGeneratorService statementGeneratorService)
    {
        _statementGeneratorService = statementGeneratorService;
    }

    public async Task Consume(ConsumeContext<TaxInfoReported> context)
    {
        await _statementGeneratorService.RecordTaxInfoAsync(context.Message);
        await context.Publish(new GenerateTaxStatement(context.Message.Cpr));
    }
}
