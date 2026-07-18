namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record ReportSalary(string Cvr, int Year, string Cpr, decimal Income);
