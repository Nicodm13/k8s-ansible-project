using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;

namespace TaxSystem.CitizenService.Consumers;

public sealed class CitizenDeregistrationRequestedConsumer : IConsumer<CitizenDeregistrationRequested>
{
    private readonly Services.CitizenService _citizenService;

    public CitizenDeregistrationRequestedConsumer(Services.CitizenService citizenService)
    {
        _citizenService = citizenService;
    }

    public async Task Consume(ConsumeContext<CitizenDeregistrationRequested> context)
    {
        await _citizenService.DeregisterCitizenAsync(context.Message.Cpr);
        var citizenDeregistered = new CitizenDeregistered(context.Message.Cpr);

        if (context.ResponseAddress is not null)
        {
            await context.RespondAsync(citizenDeregistered);
        }

        await context.Publish(citizenDeregistered);
    }
}
