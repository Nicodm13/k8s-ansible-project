using TaxSystem.Shared.Models;
using TaxSystem.Client.Services;

namespace TaxSystem.Client.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("Citizen")]
public class CitizenController : ControllerBase
{
    private readonly CitizenService _citizenService;

    public CitizenController(CitizenService citizenService)
    {
        _citizenService = citizenService;
    }

    [HttpGet("{citizenId}/Statements/{statementId}")]
    public ActionResult<Statement> GetStatement(int citizenId, int statementId)
    {
        if (citizenId <= 0 || statementId <= 0)
        {
            return BadRequest("Citizen ID and Statement ID must be greater than 0.");
        }

        try
        {
            var statement = _citizenService.GetStatementByCitizenIdAndYear(citizenId, statementId);
            return Ok(statement);
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Statement retrieval is not implemented yet.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch statement.");
        }
    }

    [HttpGet("{citizenId}/Statements/latest")]
    public ActionResult<Statement> GetStatementLatest(int citizenId)
    {
        if (citizenId <= 0)
        {
            return BadRequest("Citizen ID must be greater than 0.");
        }

        try
        {
            var statement = _citizenService.GetStatementByCitizenIdAndYear(citizenId, DateTime.Now.Year);
            return Ok(statement);
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Latest statement retrieval is not implemented yet.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch latest statement.");
        }
    }

    [HttpPost("{citizenId}/income/{year}")]
    public IActionResult ReportIncome(int citizenId, int year, [FromBody] int income)
    {
        if (citizenId <= 0 || year <= 0 || income < 0)
        {
            return BadRequest("Citizen ID, year, and income must be valid.");
        }

        try
        {
            _citizenService.ReportIncome(citizenId, year, income);
            return Ok("Income reported successfully.");
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Income reporting is not implemented yet.");
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to report income.");
        }
    }

    [HttpPost("{citizenId}/deductibles/{year}")]
    public IActionResult ReportDeductible(int citizenId, int year, [FromBody] List<Deductible> deductibles)
    {
        if (citizenId <= 0 || year <= 0 || deductibles == null || deductibles.Count == 0)
        {
            return BadRequest("Citizen ID, year, and deductibles must be valid.");
        }

        try
        {
            _citizenService.ReportDeductibles(citizenId, year, deductibles);
            return Ok("Deductibles reported successfully.");
        }
        catch (NotImplementedException)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, "Deductible reporting is not implemented yet.");
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
            var result = await _citizenService.createCitizen(citizen);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to register citizen.");
        }
    }
}