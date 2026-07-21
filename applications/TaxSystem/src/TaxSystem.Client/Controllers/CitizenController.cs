using MassTransit;
using TaxSystem.Shared.Models;
using TaxSystem.Client.Services;

namespace TaxSystem.Client.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("Citizen")]
public class CitizenController : ControllerBase
{
    private readonly CitizenClientService _citizenClientService;

    public CitizenController(CitizenClientService citizenClientService)
    {
        _citizenClientService = citizenClientService;
    }

    [HttpGet("{cpr}")]
    public async Task<ActionResult<Citizen>> GetCitizenInfo(string cpr)
    {
        if (string.IsNullOrWhiteSpace(cpr))
        {
            return BadRequest("CPR is required.");
        }

        try
        {
            var citizen = await _citizenClientService.GetCitizenByCpr(cpr);
            if (citizen is null)
            {
                return NotFound($"Citizen with CPR '{cpr}' was not found.");
            }

            return Ok(citizen);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch citizen info.");
        }
    }

    // DEPRECIATED -- ONLY COMPANIES CAN REPORT INCOME FOR THEIR EMPLOYEES
    // [HttpPost("{citizenId}/income/{year}")] 
    // public async Task<IActionResult> ReportIncome(int citizenId, int year, [FromBody] int income)
    // {
    //     if (citizenId <= 0 || year <= 0 || income < 0)
    //     {
    //         return BadRequest("Citizen ID, year, and income must be valid.");
    //     }
    //
    //     try
    //     {
    //         await _citizenClientService.ReportIncome(citizenId, year, income);
    //         return Ok("Income reported successfully.");
    //     }
    //     catch (NotImplementedException)
    //     {
    //         return StatusCode(StatusCodes.Status501NotImplemented, "Income reporting is not implemented yet.");
    //     }
    //     catch (Exception)
    //     {
    //         return StatusCode(StatusCodes.Status500InternalServerError, "Failed to report income.");
    //     }
    // }

    [HttpPost("{cpr}/deductibles/{year}")]
    public async Task<IActionResult> ReportDeductible(string cpr, int year, [FromBody] List<Deductible> deductibles)
    {
        if (string.IsNullOrWhiteSpace(cpr) || year <= 0 || deductibles == null || deductibles.Count == 0)
        {
            return BadRequest("CPR, year, and deductibles must be valid.");
        }

        try
        {
            var success = await _citizenClientService.ReportDeductibles(cpr, year, deductibles);
            if (!success)
            {
                return StatusCode(StatusCodes.Status502BadGateway, "Failed to report deductibles.");
            }

            return Ok("Deductibles reported successfully.");
        }
        catch (RequestTimeoutException ex)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to report deductibles.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> RegisterCitizen([FromBody] Citizen citizen)
    {
        try
        {
            var result = await _citizenClientService.createCitizen(citizen);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (RequestTimeoutException ex)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to register citizen.");
        }
    }

    [HttpDelete("{cpr}")]
    public async Task<IActionResult> DeregisterCitizen(string cpr)
    {
        if (string.IsNullOrWhiteSpace(cpr))
        {
            return BadRequest("CPR is required.");
        }

        try
        {
            await _citizenClientService.DeregisterCitizen(cpr);
            return Ok("Citizen deregistration requested.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to deregister citizen.");
        }
    }
}
