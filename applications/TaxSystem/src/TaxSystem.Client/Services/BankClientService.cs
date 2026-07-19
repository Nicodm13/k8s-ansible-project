using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class BankClientService
{
    private readonly IRequestClient<BankTransferInfoRequested> _bankTransferInfoClient;

    public BankClientService(IRequestClient<BankTransferInfoRequested> bankTransferInfoClient)
    {
        _bankTransferInfoClient = bankTransferInfoClient;
    }

    public async Task<BankTransfer?> GetTransferByCpr(string cpr)
    {
        var response = await _bankTransferInfoClient.GetResponse<BankTransferInfoReceived, BankTransferInfoNotFound>(
            new BankTransferInfoRequested(cpr));

        if (response.Is(out Response<BankTransferInfoReceived>? bankTransferInfoReceived))
        {
            return new BankTransfer(
                bankTransferInfoReceived.Message.Cpr,
                bankTransferInfoReceived.Message.Amount,
                bankTransferInfoReceived.Message.AccountNumber,
                bankTransferInfoReceived.Message.RegistrationNumber,
                bankTransferInfoReceived.Message.Status);
        }

        return null;
    }
}
