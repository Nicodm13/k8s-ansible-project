using TaxSystem.Client.Services;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("Government")]
public class GovernmentController : ControllerBase
{
    private readonly CitizenService _citizenService;
    private readonly CompanyService _companyService;

    public GovernmentController(CitizenService citizenService, CompanyService companyservice)
    {
        _citizenService = citizenService;
        _companyService = companyservice;
    }

    [HttpPost("Citizens/{cpr}")]
    public ActionResult RegisterCitizen(Citizen citizen)
    {
        try
        {
            _citizenService.createCitizen(citizen);
            return Ok("Citizen registered successfully.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create Citizen.");
        }
    }

    [HttpPost("Companies/{cvr}")]
    public ActionResult RegisterCompany(Company company)
    {
        try
        {
            _companyService.RegisterCompany(company);
            return Ok("Company registered successfully.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to create Citizen.");
        }
    }

}
