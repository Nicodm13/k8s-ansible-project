namespace TaxSystem.Shared.Models;

/// <summary>
/// A deductible expense reported by a citizen (e.g. charitable donations, commuting costs).
/// Only a percentage of the reported amount is actually deductible; the percentage for each
/// <see cref="DeductionType"/> is applied by TaxSystem.StatementGeneratorService when the
/// deduction is recorded against the citizen's tax statement.
/// </summary>
public class Deductible
{
    public decimal Amount { get; set; }

    public string DeductionType { get; set; } = string.Empty;
}

