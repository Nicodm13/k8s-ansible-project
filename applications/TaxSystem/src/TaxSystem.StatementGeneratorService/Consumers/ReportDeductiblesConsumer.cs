using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendStatementGeneratorService = TaxSystem.StatementGenerator.Services.StatementGeneratorService;

namespace TaxSystem.StatementGenerator.Consumers;

/// <summary>
/// Handles a citizen reporting a deductible expense. Mirrors <see cref="TaxInfoReportedConsumer"/>:
/// persists the (percentage-adjusted) deductible amount against the citizen's tax info, triggers
/// statement regeneration, and responds once the report has been recorded so the caller can await
/// confirmation - exactly like a company reporting an employee's salary awaits <c>SalaryReported</c>.
/// </summary>
public class ReportDeductiblesConsumer : IConsumer<ReportDeductibles>
{
    private readonly BackendStatementGeneratorService _statementGeneratorService;

    public ReportDeductiblesConsumer(BackendStatementGeneratorService statementGeneratorService)
    {
        _statementGeneratorService = statementGeneratorService;
    }

    public async Task Consume(ConsumeContext<ReportDeductibles> context)
    {
        var deductibleAmount = BackendStatementGeneratorService.CalculateDeductibleAmount(
            context.Message.Amount,
            context.Message.DeductionType);

        await _statementGeneratorService.RecordTaxInfoAsync(new TaxInfoReported(
            context.Message.Cpr,
            null,
            null,
            null,
            deductibleAmount,
            null));

        await context.Publish(new GenerateTaxStatement(context.Message.Cpr));

        await context.RespondAsync(new DeductiblesReported(
            context.Message.Cpr,
            deductibleAmount,
            context.Message.DeductionType));
    }
}

