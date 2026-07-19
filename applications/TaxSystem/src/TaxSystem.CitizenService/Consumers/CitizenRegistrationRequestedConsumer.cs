using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;
using BackendCitizenService = TaxSystem.CitizenService.Services.CitizenService;

namespace TaxSystem.CitizenService.Consumers;

public sealed class CitizenRegistrationRequestedConsumer : IConsumer<CitizenRegistrationRequested>
{
    private readonly BackendCitizenService _citizenService;

    public CitizenRegistrationRequestedConsumer(BackendCitizenService citizenService)
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
            $"{citizen.firstName} {citizen.lastName}".Trim());

        if (context.ResponseAddress is not null)
        {
            await context.RespondAsync(citizenRegistered);
        }

        await context.Publish(citizenRegistered);
    }
}

