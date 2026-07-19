using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendBankService = TaxSystem.BankService.Services.BankService;

namespace TaxSystem.BankService.Consumers;

public class ScheduleBankTransferConsumer : IConsumer<ScheduleBankTransfer>
{
    private readonly BackendBankService _bankService;

    public ScheduleBankTransferConsumer(BackendBankService bankService)
    {
        _bankService = bankService;
    }

    public async Task Consume(ConsumeContext<ScheduleBankTransfer> context)
    {
        var transfer = await _bankService.ScheduleTransferAsync(context.Message);
        var bankTransferScheduled = new BankTransferScheduled(
            transfer.Cpr,
            transfer.Amount,
            transfer.AccountNumber,
            transfer.RegistrationNumber);

        if (context.ResponseAddress is not null)
        {
            await context.RespondAsync(bankTransferScheduled);
        }

        await context.Publish(bankTransferScheduled);
    }
}
