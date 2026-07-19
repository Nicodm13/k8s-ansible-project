using Microsoft.AspNetCore.Mvc;
using TaxSystem.Client.Services;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Controllers;

[ApiController]
[Route("Bank")]
public class BankController : ControllerBase
{
    private readonly BankClientService _bankClientService;

    public BankController(BankClientService bankClientService)
    {
        _bankClientService = bankClientService;
    }

    [HttpGet("transfers/{cpr}")]
    public async Task<ActionResult<BankTransfer>> GetTransfer(string cpr)
    {
        if (string.IsNullOrWhiteSpace(cpr))
        {
            return BadRequest("CPR is required.");
        }

        try
        {
            var transfer = await _bankClientService.GetTransferByCpr(cpr);
            if (transfer is null)
            {
                return NotFound($"Bank transfer for citizen '{cpr}' was not found.");
            }

            return Ok(transfer);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to fetch bank transfer.");
        }
    }
}
