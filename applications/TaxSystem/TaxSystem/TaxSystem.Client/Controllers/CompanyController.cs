using TaxSystem.Client.Services;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Controllers;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// A simple API to allow Companies to interface with the tax system.
/// No authentication is implemented here for simplicity, so we assume that the client
/// is trusted and that the Company is authenticated and allowed to access the requested endpoints.
/// </summary>
[ApiController]
[Route("Company")]
public class CompanyController : ControllerBase
{
    private readonly CompanyClientService _companyService;

    public CompanyController(CompanyClientService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("{cvr}")]
    public async Task<ActionResult<Company>> GetCompanyInfo(string cvr)
    {
        if (string.IsNullOrWhiteSpace(cvr))
        {
            return BadRequest("CVR is required.");
        }

        try
        {
            var company = await _companyService.getCompanyFromCvr(cvr);
            if (company is null)
            {
                return NotFound($"Company with CVR '{cvr}' was not found.");
            }

            return Ok(company);
        }

        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Company lookup is not implemented yet.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch company info.");
        }
    }

    [HttpPost("{cvr}/employees/income/{year}/{cpr}")]
    public async Task<ActionResult> SetEmployeeIncomeForYear(string cvr, int year, int cpr, [FromBody] int income)
    {
        if (string.IsNullOrWhiteSpace(cvr) || year <= 0 || cpr <= 0 || income < 0)
        {
            return BadRequest("CVR, year, CPR, and income must be valid.");
        }

        try
        {
            await _companyService.SetEmployeeIncomeForYear(cvr, year, cpr, income);
            return Ok("Employee income reported successfully.");
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Employee income reporting is not implemented yet.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to report employee income.");
        }
    }

    [HttpPost]
    public async Task<ActionResult> RegisterCompany([FromBody] Company company)
    {
        if (company is null || string.IsNullOrWhiteSpace(company.CVR) || string.IsNullOrWhiteSpace(company.Name))
        {
            return BadRequest("Company CVR and name are required.");
        }

        await _companyService.RegisterCompany(company);
        return Accepted("Company registration requested.");
    }

    [HttpPut]
    public async Task<ActionResult> UpdateCompany([FromBody] Company company)
    {
        if (company is null || string.IsNullOrWhiteSpace(company.CVR) || string.IsNullOrWhiteSpace(company.Name))
        {
            return BadRequest("Company CVR and name are required.");
        }

        await _companyService.UpdateCompany(company);
        return Accepted("Company update requested.");
    }

}
