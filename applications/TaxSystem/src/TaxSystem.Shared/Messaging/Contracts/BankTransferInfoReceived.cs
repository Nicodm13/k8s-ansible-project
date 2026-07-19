namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record BankTransferInfoReceived(
    string Cpr,
    decimal Amount,
    string AccountNumber,
    string RegistrationNumber,
    string Status);
