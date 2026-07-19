using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using BackendCitizenService = TaxSystem.CitizenService.Services.CitizenService;

namespace TaxSystem.CitizenService.Consumers;

public sealed class CitizenInfoRequestedConsumer : IConsumer<CitizenInfoRequested>
{
    private readonly BackendCitizenService _citizenService;

    public CitizenInfoRequestedConsumer(BackendCitizenService citizenService)
    {
        _citizenService = citizenService;
    }

    public async Task Consume(ConsumeContext<CitizenInfoRequested> context)
    {
        var citizen = await _citizenService.GetByCprAsync(context.Message.Cpr);
        if (citizen is null)
        {
            await context.RespondAsync(new CitizenInfoNotFound(context.Message.Cpr));
            return;
        }

        await context.RespondAsync(new CitizenInfoReceived(
            citizen.cpr,
            citizen.firstName,
            citizen.lastName,
            citizen.streetAddress,
            citizen.city,
            citizen.zipCode,
            citizen.bankAccountNumber));
    }
}
