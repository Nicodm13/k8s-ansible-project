namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record ReportSalary(string Cvr, string Cpr, string Name, decimal Salary);
