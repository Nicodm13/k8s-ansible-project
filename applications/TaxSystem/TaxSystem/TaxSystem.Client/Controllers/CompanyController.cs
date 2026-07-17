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
    private readonly CompanyService _companyService;

    public CompanyController(CompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet("{cvr}")]
    public ActionResult<Company> GetCompanyInfo(string cvr)
    {
        if (string.IsNullOrWhiteSpace(cvr))
        {
            return BadRequest("CVR is required.");
        }

        try
        {
            var company = _companyService.getCompanyFromCvr(cvr);
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
}