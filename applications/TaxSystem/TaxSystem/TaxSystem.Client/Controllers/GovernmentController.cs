using TaxSystem.Client.Services;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("Government")]
public class GovernmentController : ControllerBase
{
    private readonly AuditService _auditService;
    private readonly CitizenService _citizenService;
    private readonly CompanyService _companyService;

    public GovernmentController(AuditService auditService,  CitizenService citizenService, CompanyService companyservice)
    {
        _auditService = auditService;
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

    [HttpGet("Audits/Citizens/{cpr}")]
    public ActionResult<IEnumerable<Audit>> GetCitizenAudits(string cpr)
    {
        if (string.IsNullOrWhiteSpace(cpr))
        {
            return BadRequest("CPR is required.");
        }

        try
        {
            var audits = _auditService.GetCitizenAuditsByCpr(cpr);
            return Ok(audits);
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Citizen audit lookup is not implemented yet.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch citizen audits.");
        }
    }

    [HttpGet("Audits/Citizens/{cpr}/{year}")]
    public ActionResult<Audit> GetCitizenAudit(string cpr, string year)
    {
        if (string.IsNullOrWhiteSpace(cpr) || string.IsNullOrWhiteSpace(year))
        {
            return BadRequest("CPR and year are required.");
        }

        try
        {
            var audit = _auditService.GetCitizenAuditByCprAndYear(cpr, year);
            return Ok(audit);
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Citizen yearly audit lookup is not implemented yet.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch citizen audit.");
        }
    }
}