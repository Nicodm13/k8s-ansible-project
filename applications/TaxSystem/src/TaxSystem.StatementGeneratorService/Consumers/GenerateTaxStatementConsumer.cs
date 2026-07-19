using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendStatementGeneratorService = TaxSystem.StatementGenerator.Services.StatementGeneratorService;

namespace TaxSystem.StatementGenerator.Consumers;

public class GenerateTaxStatementConsumer : IConsumer<GenerateTaxStatement>
{
    private readonly BackendStatementGeneratorService _statementGeneratorService;
    private readonly IRequestClient<CitizenInfoRequested> _citizenInfoClient;

    public GenerateTaxStatementConsumer(
        BackendStatementGeneratorService statementGeneratorService,
        IRequestClient<CitizenInfoRequested> citizenInfoClient)
    {
        _statementGeneratorService = statementGeneratorService;
        _citizenInfoClient = citizenInfoClient;
    }

    public async Task Consume(ConsumeContext<GenerateTaxStatement> context)
    {
        var result = await _statementGeneratorService.GenerateStatementAsync(context.Message.Cpr, _citizenInfoClient);
        if (result.StatementGenerated is not null)
        {
            await context.Publish(result.StatementGenerated);
            if (context.ResponseAddress is not null)
            {
                await context.RespondAsync(result.StatementGenerated);
            }

            if (result.ScheduleBankTransfer is not null)
            {
                await context.Publish(result.ScheduleBankTransfer);
            }

            return;
        }

        await context.Publish(result.StatementNotReady!);
        if (context.ResponseAddress is not null)
        {
            await context.RespondAsync(result.StatementNotReady!);
        }
    }
}
