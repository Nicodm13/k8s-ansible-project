namespace TaxSystem.Shared.Models;

public class Statement
{
    public string? reportId { get; set; }
    public DateTime reportedAt { get; set; }
    public string? name { get; set; }
    public string? cpr { get; set; }
    public string? employerName { get; set; }
    public string? annualGrossSalary { get; set; }
    public string? annualCapitalGains { get; set; }
    public string? annualTotalDeduction { get; set; }
    public string? annualPaidTax { get; set; }
    public string? annualTax { get; set; }
    public string? annualOwedTax { get; set; }
}
