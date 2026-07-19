using System.Globalization;
using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Client.Services;

public class StatementGeneratorClientService
{
    private readonly IRequestClient<GenerateTaxStatement> _generateTaxStatementClient;

    public StatementGeneratorClientService(IRequestClient<GenerateTaxStatement> generateTaxStatementClient)
    {
        _generateTaxStatementClient = generateTaxStatementClient;
    }

    public async Task<Statement?> GetStatementByCprAndYear(string cpr, int year)
    {
        var response = await _generateTaxStatementClient.GetResponse<StatementGenerated, StatementNotReady>(
            new GenerateTaxStatement(cpr));

        if (response.Is(out Response<StatementGenerated>? statementGenerated))
        {
            return new Statement
            {
                name = statementGenerated.Message.Name,
                annualGrossSalary = FormatDecimal(statementGenerated.Message.AnnualGrossSalary),
                annualCapitalGains = FormatDecimal(statementGenerated.Message.AnnualCapitalGains),
                annualTotalDeduction = FormatDecimal(statementGenerated.Message.AnnualTotalDeduction),
                annualPaidTax = FormatDecimal(statementGenerated.Message.AnnualPaidTax),
                annualTax = FormatDecimal(statementGenerated.Message.AnnualTax),
                annualOwedTax = FormatDecimal(statementGenerated.Message.AnnualOwedTax)
            };
        }

        return null;
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.#############################", CultureInfo.InvariantCulture);
    }
}
