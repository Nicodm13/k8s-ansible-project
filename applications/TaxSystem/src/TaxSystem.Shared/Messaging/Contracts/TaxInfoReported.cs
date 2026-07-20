namespace TaxSystem.Shared.Messaging.Contracts;

public sealed record TaxInfoReported(
    string Cpr,
    string? Name,
    decimal? AnnualGrossSalary,
    decimal? AnnualCapitalGains,
    decimal? AnnualTotalDeduction,
    decimal? AnnualPaidTax);
