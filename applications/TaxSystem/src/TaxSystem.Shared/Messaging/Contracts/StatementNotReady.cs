namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record StatementNotReady(string Cpr, string Reason);
