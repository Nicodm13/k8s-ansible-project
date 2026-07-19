using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.CitizenService.Consumers;

public sealed class CitizenRegistrationRequestedConsumer : IConsumer<CitizenRegistrationRequested>
{
    private readonly Services.CitizenService _citizenService;

    public CitizenRegistrationRequestedConsumer(Services.CitizenService citizenService)
    {
        _citizenService = citizenService;
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

        await _citizenService.RegisterCitizenAsync(citizen);

        var citizenRegistered = new CitizenRegistered(
            citizen.cpr,
            $"{citizen.firstName} {citizen.lastName}");

        if (context.ResponseAddress is not null)
        {
            await context.RespondAsync(citizenRegistered);
        }

        await context.Publish(citizenRegistered);
    }
}
