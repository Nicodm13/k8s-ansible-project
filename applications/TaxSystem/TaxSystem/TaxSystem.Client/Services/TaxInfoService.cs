using System.Collections.Concurrent;
using TaxSystem.Shared.Models;
using TaxSystem.Shared.Messaging.Contracts;

namespace TaxSystem.Client.Services;

public class TaxInfoService
{
    private readonly ConcurrentDictionary<string, string> _citizenNames = new();
    private readonly ConcurrentDictionary<string, Statement> _statements = new();

    public void RegisterCitizen(Citizen citizen)
    {
        _citizenNames[citizen.cpr] = $"{citizen.firstName} {citizen.lastName}";
    }

    public void setIncomeByCprAndYear(int income, int cpr, int year, string source)
    {
        SaveStatement(cpr.ToString(), year, income);
    }

    public void setDeductiblesByCprAndYear(IEnumerable<Deductible> deductibles, int cpr, int year, string source)
    {
        throw new NotImplementedException();
    }

    public void getNewestTaxStatementByCprAndYear(string cpr, int year)
    {
    }

    public Statement GetStatementByCprAndYear(string cpr, int year)
    {
        return _statements.TryGetValue(GetStatementKey(cpr, year), out var statement)
            ? statement
            : throw new KeyNotFoundException($"No statement found for CPR '{cpr}' and year '{year}'.");
    }

    public void RecordTaxInfo(TaxInfoReported taxInfo)
    {
        SaveStatement(taxInfo.Cpr, DateTime.Now.Year, taxInfo.AnnualGrossSalary);
    }

    private void SaveStatement(string cpr, int year, decimal annualGrossSalary)
    {
        _statements[GetStatementKey(cpr, year)] = new Statement
        {
            name = _citizenNames.GetValueOrDefault(cpr, string.Empty),
            annualGrossSalary = annualGrossSalary.ToString(),
            annualCapitalGains = "0",
            annualTotalDeduction = "0",
            annualPaidTax = "0",
            annualTax = "0",
            annualOwedTax = "0"
        };
    }

    private static string GetStatementKey(string cpr, int year)
    {
        return $"{cpr}:{year}";
    }
}
