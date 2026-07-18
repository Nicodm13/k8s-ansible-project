namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record CitizenUpdated(
    string Cpr,
    string FirstName,
    string LastName,
    string StreetAddress,
    string City,
    string ZipCode,
    string BankAccountNumber);
