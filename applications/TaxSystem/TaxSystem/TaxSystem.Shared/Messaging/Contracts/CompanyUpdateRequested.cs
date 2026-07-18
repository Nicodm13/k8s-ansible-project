namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record CompanyUpdateRequested(string Cvr, string Name);
