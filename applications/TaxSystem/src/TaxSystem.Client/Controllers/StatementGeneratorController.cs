using Microsoft.AspNetCore.Mvc;
using TaxSystem.Client.Services;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Controllers;

[ApiController]
[Route("StatementGenerator")]
public class StatementGeneratorController : ControllerBase
{
    private readonly StatementGeneratorClientService _statementGeneratorClientService;

    public StatementGeneratorController(StatementGeneratorClientService statementGeneratorClientService)
    {
        _statementGeneratorClientService = statementGeneratorClientService;
    }

    [HttpGet("{cpr}/Statements/{year}")]
    public async Task<ActionResult<Statement>> GetStatement(string cpr, int year)
    {
        if (string.IsNullOrWhiteSpace(cpr) || year <= 0)
        {
            return BadRequest("CPR and year must be valid.");
        }

        try
        {
            var statement = await _statementGeneratorClientService.GetStatementByCprAndYear(cpr, year);
            if (statement is null)
            {
                return NotFound($"Statement for citizen '{cpr}' and year '{year}' is not ready.");
            }

            return Ok(statement);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch statement.");
        }
    }
}
