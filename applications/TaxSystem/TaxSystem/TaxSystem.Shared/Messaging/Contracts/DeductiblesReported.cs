namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record DeductiblesReported(string Cpr, decimal Amount, string DeductionType);
