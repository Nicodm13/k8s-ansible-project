namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record BankTransferScheduled(string Cpr, decimal Amount, string AccountNumber, string RegistrationNumber);
