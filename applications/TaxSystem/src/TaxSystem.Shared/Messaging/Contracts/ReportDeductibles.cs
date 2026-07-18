namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record ReportDeductibles(string Cpr, decimal Amount, string DeductionType);
