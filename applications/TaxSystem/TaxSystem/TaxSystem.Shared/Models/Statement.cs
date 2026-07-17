namespace TaxSystem.Shared.Models;

public class Statement
{
    public string annualGrossSalary { get; set; }
    public string annualCapitalGains { get; set; }
    public string annualTotalDeduction { get; set; }
    public string annualPaidTax { get; set; }
    public string annualTax { get; set; }
    public string annualOwedTax { get; set; }
}