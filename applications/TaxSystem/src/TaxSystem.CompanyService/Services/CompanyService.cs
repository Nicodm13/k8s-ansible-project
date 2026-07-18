namespace TaxSystem.CompanyService.Services;

using TaxSystem.CompanyService.Repositories;
using TaxSystem.Shared.Models;

public class CompanyService
{
    private readonly IReadCompanyRepository _readRepository;
    private readonly IWriteCompanyRepository _writeRepository;

    public CompanyService(
        IReadCompanyRepository readRepository,
        IWriteCompanyRepository writeRepository)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
    }

    public Task<Company?> GetByCvrAsync(string cvr)
    {
        return _readRepository.GetByCvrAsync(cvr);
    }

    public async Task<Company> RegisterCompanyAsync(string cvr, string name)
    {
        var company = new Company
        {
            CVR = cvr,
            Name = name
        };

        await _writeRepository.SaveAsync(company);
        return company;
    }

    public async Task<Company> UpdateCompanyAsync(string cvr, string name)
    {
        var company = new Company
        {
            CVR = cvr,
            Name = name
        };

        await _writeRepository.SaveAsync(company);
        return company;
    }

    public Task<bool> DeregisterCompanyAsync(string cvr)
    {
        return _writeRepository.DeleteAsync(cvr);
    }
}
