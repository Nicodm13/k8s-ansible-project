using TaxSystem.BankService.Repositories;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.BankService.Services;

public class BankService
{
    private readonly IBankReadRepository _readRepository;
    private readonly IBankWriteRepository _writeRepository;

    public BankService(
        IBankReadRepository readRepository,
        IBankWriteRepository writeRepository)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
    }

    public async Task<BankTransfer> ScheduleTransferAsync(ScheduleBankTransfer request)
    {
        var transfer = new BankTransfer(
            request.Cpr,
            request.Amount,
            request.AccountNumber,
            request.RegistrationNumber,
            "Scheduled");

        await _writeRepository.SaveAsync(transfer);

        return transfer;
    }

    public Task<BankTransfer?> GetByCprAsync(string cpr)
    {
        return _readRepository.GetByCprAsync(cpr);
    }
}
