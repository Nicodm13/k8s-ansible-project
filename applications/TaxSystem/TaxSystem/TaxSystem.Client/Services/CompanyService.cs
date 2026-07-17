using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class CompanyService
{
    private readonly TaxInfoService _taxInfoService;

    public CompanyService(TaxInfoService taxInfoService)
    {
        _taxInfoService = taxInfoService;
    }

    public Company getCompanyFromCvr(string cvr)
    {
        throw new NotImplementedException();
    }

    public void SetEmployeeIncomeForYear(string cvr, int year, int cpr, int income)
    {
        _taxInfoService.setIncomeByCprAndYear(income, cpr, year, $"Company-{cvr}");
    }
}