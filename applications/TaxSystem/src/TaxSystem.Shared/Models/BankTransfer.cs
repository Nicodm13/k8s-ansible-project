namespace TaxSystem.Shared.Models;

public sealed record BankTransfer(
    string Cpr,
    decimal Amount,
    string AccountNumber,
    string RegistrationNumber,
    string Status);
