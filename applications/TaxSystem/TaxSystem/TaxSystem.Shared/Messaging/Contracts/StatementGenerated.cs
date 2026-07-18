namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record StatementGenerated(
    string Cpr,
    string Name,
    decimal AnnualGrossSalary,
    decimal AnnualCapitalGains,
    decimal AnnualTotalDeduction,
    decimal AnnualPaidTax,
    decimal AnnualTax,
    decimal AnnualOwedTax);
