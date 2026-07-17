using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class CitizenService
{
    private readonly TaxInfoService _taxInfoService;

    public CitizenService(TaxInfoService taxInfoService)
    {
        _taxInfoService = taxInfoService;
    }

    public Statement GetStatementByCitizenIdAndYear(int citizenId, int year)
    {
        throw new NotImplementedException();
    }
    

    public void ReportIncome(int citizenId, int year, int income)
    {
        _taxInfoService.setIncomeByCprAndYear(income, citizenId, year, "CitizenReport");
    }

    public void ReportDeductibles(int citizenId, int year, IEnumerable<Deductible> deductibles)
    {
        _taxInfoService.setDeductiblesByCprAndYear(deductibles, citizenId, year, "CitizenReport");
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="citizen"></param>
    /// <exception cref="InvalidOperationException">thrown if attempting to create a citizen that already exists</exception>
    public void createCitizen(Citizen citizen)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="citizen"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void updateCitizen(Citizen? citizen)
    {
        throw new NotImplementedException();
    }
}


