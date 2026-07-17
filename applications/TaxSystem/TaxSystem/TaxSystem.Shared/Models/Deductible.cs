namespace TaxSystem.Shared.Models;

public class Deductible
{
    int _amount;
    public double deductionPercentage;

    internal class CharitableDonations : Deductible
    {
        public readonly double deductionPercentage = 0.5;
        public CharitableDonations(int amount) { _amount = amount; }
    }

    internal class Commuting : Deductible
    {
        public readonly double deductionPercentage = 0.3;
        public Commuting(int amount) { _amount = amount; }
    }

}