using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Controllers;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("Citizen")]
public class CitizenController
{
    [HttpGet("{citizenId}/Statements/{statementId}")]
    public IActionResult GetStatement(int citizenId, int statementId)
    {
        throw new NotImplementedException();
    }
    [HttpGet("{citizenId}/Statements/latest")]
    public IActionResult GetStatementLatest(int citizenId)
    {
        throw new NotImplementedException();
    }
    [HttpPost("{citizenId}/income/{year}")]
    public IActionResult ReportIncome(int citizenId, int year, [FromBody]string income)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost("{citizenId}/deductibles/{year}")]
    public IActionResult ReportDeductible(int citizenId, int year, [FromBody]List<Deductible> deductibles)
    {
        throw new NotImplementedException();
    }
}