namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record ScheduleBankTransfer(string Cpr, decimal Amount, string AccountNumber, string RegistrationNumber);
