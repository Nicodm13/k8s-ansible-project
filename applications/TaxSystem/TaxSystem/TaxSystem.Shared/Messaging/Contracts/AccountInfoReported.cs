namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record AccountInfoReported(string Cpr, string AccountNumber, string RegistrationNumber);
