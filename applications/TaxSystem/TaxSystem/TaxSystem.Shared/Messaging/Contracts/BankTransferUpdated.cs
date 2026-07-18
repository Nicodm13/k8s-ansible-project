namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record BankTransferUpdated(string Cpr, decimal Amount, string Status);
