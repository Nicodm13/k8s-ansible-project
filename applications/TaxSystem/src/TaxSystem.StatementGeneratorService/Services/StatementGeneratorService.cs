using System.Globalization;
using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;
using TaxSystem.StatementGenerator.Repositories;

namespace TaxSystem.StatementGenerator.Services;

public class StatementGeneratorService
{
    private const decimal TaxRate = 0.37m;
    private readonly IReadStatementRepository _readStatementRepository;
    private readonly IWriteStatementRepository _writeStatementRepository;

    public StatementGeneratorService(
        IReadStatementRepository readStatementRepository,
        IWriteStatementRepository writeStatementRepository)
    {
        _readStatementRepository = readStatementRepository;
        _writeStatementRepository = writeStatementRepository;
    }

    public async Task RecordTaxInfoAsync(TaxInfoReported taxInfo)
    {
        var statement = new Statement
        {
            name = taxInfo.Name,
            annualGrossSalary = FormatDecimal(taxInfo.AnnualGrossSalary),
            annualCapitalGains = FormatDecimal(taxInfo.AnnualCapitalGains),
            annualTotalDeduction = FormatDecimal(taxInfo.AnnualTotalDeduction),
            annualPaidTax = FormatDecimal(taxInfo.AnnualPaidTax),
            annualTax = string.Empty,
            annualOwedTax = string.Empty
        };

        await _writeStatementRepository.SaveAsync(taxInfo.Cpr, statement);
    }

    public async Task<StatementGenerationResult> GenerateStatementAsync(
        string cpr,
        IRequestClient<CitizenInfoRequested> citizenInfoClient)
    {
        var statement = await _readStatementRepository.GetByCprAsync(cpr);
        if (statement is null)
        {
            return StatementGenerationResult.NotReady(new StatementNotReady(cpr, "Tax info has not been reported"));
        }

        var citizenResponse = await citizenInfoClient.GetResponse<CitizenInfoReceived, CitizenInfoNotFound>(
            new CitizenInfoRequested(cpr));
        if (!citizenResponse.Is(out Response<CitizenInfoReceived>? citizenInfoReceived))
        {
            return StatementGenerationResult.NotReady(new StatementNotReady(cpr, "Citizen info has not been received"));
        }

        var name = $"{citizenInfoReceived.Message.FirstName} {citizenInfoReceived.Message.LastName}".Trim();
        var grossSalary = ParseDecimal(statement.annualGrossSalary);
        var capitalGains = ParseDecimal(statement.annualCapitalGains);
        var totalDeduction = ParseDecimal(statement.annualTotalDeduction);
        var paidTax = ParseDecimal(statement.annualPaidTax);
        var annualTax = Math.Max(0m, (grossSalary + capitalGains - totalDeduction) * TaxRate);
        var owedTax = annualTax - paidTax;

        statement.name = string.IsNullOrWhiteSpace(statement.name) ? name : statement.name;
        statement.annualTax = FormatDecimal(annualTax);
        statement.annualOwedTax = FormatDecimal(owedTax);
        await _writeStatementRepository.SaveAsync(cpr, statement);

        var generated = new StatementGenerated(
            cpr,
            statement.name,
            grossSalary,
            capitalGains,
            totalDeduction,
            paidTax,
            annualTax,
            owedTax);

        var transfer = owedTax < 0m
            ? new ScheduleBankTransfer(cpr, Math.Abs(owedTax), citizenInfoReceived.Message.BankAccountNumber, string.Empty)
            : null;

        return StatementGenerationResult.Generated(generated, transfer);
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string FormatDecimal(decimal value)
    {
        return value.ToString("0.#############################", CultureInfo.InvariantCulture);
    }
}

public sealed record StatementGenerationResult(
    StatementGenerated? StatementGenerated,
    StatementNotReady? StatementNotReady,
    ScheduleBankTransfer? ScheduleBankTransfer)
{
    public static StatementGenerationResult Generated(
        StatementGenerated statementGenerated,
        ScheduleBankTransfer? scheduleBankTransfer)
    {
        return new StatementGenerationResult(statementGenerated, null, scheduleBankTransfer);
    }

    public static StatementGenerationResult NotReady(StatementNotReady statementNotReady)
    {
        return new StatementGenerationResult(null, statementNotReady, null);
    }
}
