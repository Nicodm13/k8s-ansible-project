namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record CompanyRegistrationRequested(string Cvr, string Name);
