namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record SalaryReported(string Cpr, string Name, int Salary);
