using TaxSystem.Client.Services;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("Government")]
public class GovernmentController : ControllerBase
{
    private readonly CitizenClientService _citizenClientService;
    private readonly CompanyClientService _companyService;

    public GovernmentController(CitizenClientService citizenClientService, CompanyClientService companyservice)
    {
        _citizenClientService = citizenClientService;
        _companyService = companyservice;
    }

    [HttpPost("Citizens/{cpr}")]
    public async Task<ActionResult> RegisterCitizen(Citizen citizen)
    {
        try
        {
            await _citizenClientService.createCitizen(citizen);
            return Ok("Citizen registered successfully.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create Citizen.");
        }
    }

    [HttpPost("Companies/{cvr}")]
    public async Task<ActionResult> RegisterCompany(Company company)
    {
        try
        {
            await _companyService.RegisterCompany(company);
            return Accepted("Company registration requested.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create company.");
        }
    }

}
