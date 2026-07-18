namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record CitizenRegistered(string Cpr, string Name) : ICorrelatedEvent
{
    public string CorrelationKey => Cpr;
}
