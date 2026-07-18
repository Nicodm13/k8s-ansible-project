using MassTransit;
using TaxSystem.CitizenService.Repositories;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.CitizenService.Consumers;

public sealed class CitizenRegistrationRequestedConsumer : IConsumer<CitizenRegistrationRequested>
{
    private readonly IWriteCitizenRepository _writeCitizenRepository;

    public CitizenRegistrationRequestedConsumer(IWriteCitizenRepository writeCitizenRepository)
    {
        _writeCitizenRepository = writeCitizenRepository;
    }

    public async Task Consume(ConsumeContext<CitizenRegistrationRequested> context)
    {
        var message = context.Message;

        var citizen = new Citizen
        {
            cpr = message.Cpr,
            firstName = message.FirstName,
            lastName = message.LastName,
            streetAddress = message.StreetAddress,
            city = message.City,
            zipCode = message.ZipCode,
            bankAccountNumber = message.BankAccountNumber
        };

        await _writeCitizenRepository.SaveAsync(citizen);
    }
}

