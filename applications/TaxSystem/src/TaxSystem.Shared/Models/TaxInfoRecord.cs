namespace TaxSystem.Shared.Models;

public sealed record TaxInfoRecord(
    string Cpr,
    string Name,
    decimal AnnualGrossSalary,
    decimal AnnualCapitalGains,
    decimal AnnualTotalDeduction,
    decimal AnnualPaidTax);
