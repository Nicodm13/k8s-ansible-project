using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendBankService = TaxSystem.BankService.Services.BankService;

namespace TaxSystem.BankService.Consumers;

public class BankTransferInfoRequestedConsumer : IConsumer<BankTransferInfoRequested>
{
    private readonly BackendBankService _bankService;

    public BankTransferInfoRequestedConsumer(BackendBankService bankService)
    {
        _bankService = bankService;
    }

    public async Task Consume(ConsumeContext<BankTransferInfoRequested> context)
    {
        var transfer = await _bankService.GetByCprAsync(context.Message.Cpr);
        if (transfer is null)
        {
            await context.RespondAsync(new BankTransferInfoNotFound(context.Message.Cpr));
            return;
        }

        await context.RespondAsync(new BankTransferInfoReceived(
            transfer.Cpr,
            transfer.Amount,
            transfer.AccountNumber,
            transfer.RegistrationNumber,
            transfer.Status));
    }
}
