using TaxSystem.Client.Services;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("Government")]
public class GovernmentController : ControllerBase
{
    private readonly GovernmentService _governmentService;

    public GovernmentController(GovernmentService governmentService)
    {
        _governmentService = governmentService;
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
            var audits = _governmentService.GetCitizenAuditsByCpr(cpr);
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
            var audit = _governmentService.GetCitizenAuditByCprAndYear(cpr, year);
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