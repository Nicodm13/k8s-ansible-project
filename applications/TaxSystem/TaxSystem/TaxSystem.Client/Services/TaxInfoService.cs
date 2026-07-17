using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class TaxInfoService
{
    public void setIncomeByCprAndYear(int income, int cpr, int year, string source)
    {
        throw new NotImplementedException();
    }

    public void setDeductiblesByCprAndYear(IEnumerable<Deductible> deductibles, int cpr, int year, string source)
    {
        throw new NotImplementedException();
    }

    public void getNewestTaxStatementByCprAndYear(string cpr, int year)
    {
        throw new NotImplementedException();
    }
}