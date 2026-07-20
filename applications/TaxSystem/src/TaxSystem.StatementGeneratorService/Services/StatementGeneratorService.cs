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
            reportId = Guid.NewGuid().ToString("N"),
            reportedAt = DateTime.UtcNow,
            cpr = taxInfo.Cpr,
            name = taxInfo.Name,
            annualGrossSalary = FormatNullableDecimal(taxInfo.AnnualGrossSalary),
            annualCapitalGains = FormatNullableDecimal(taxInfo.AnnualCapitalGains),
            annualTotalDeduction = FormatNullableDecimal(taxInfo.AnnualTotalDeduction),
            annualPaidTax = FormatNullableDecimal(taxInfo.AnnualPaidTax)
        };

        await _writeStatementRepository.SaveReportAsync(taxInfo.Cpr, statement);
    }

    public async Task<StatementGenerationResult> GenerateStatementAsync(
        string cpr,
        IRequestClient<CitizenInfoRequested> citizenInfoClient)
    {
        var statement = await _readStatementRepository.GetMergedStatementAsync(cpr);
        if (statement is null || statement.annualGrossSalary is null || statement.annualPaidTax is null)
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
        var capitalGains = ParseDecimalOrZero(statement.annualCapitalGains);
        var totalDeduction = ParseDecimalOrZero(statement.annualTotalDeduction);
        var paidTax = ParseDecimal(statement.annualPaidTax);
        var annualTax = Math.Max(0m, (grossSalary + capitalGains - totalDeduction) * TaxRate);
        var owedTax = annualTax - paidTax;

        var finalName = string.IsNullOrWhiteSpace(statement.name) ? name : statement.name;

        var generated = new StatementGenerated(
            cpr,
            finalName,
            grossSalary,
            capitalGains,
            totalDeduction,
            paidTax,
            annualTax,
            owedTax);

        var transfer = owedTax != 0m
            ? new ScheduleBankTransfer(cpr, Math.Abs(owedTax), citizenInfoReceived.Message.BankAccountNumber, string.Empty)
            : null;

        return StatementGenerationResult.Generated(generated, transfer);
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    private static decimal ParseDecimalOrZero(string? value)
    {
        return string.IsNullOrEmpty(value) ? 0m : decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    private static string? FormatNullableDecimal(decimal? value)
    {
        return value?.ToString("0.#############################", CultureInfo.InvariantCulture);
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
