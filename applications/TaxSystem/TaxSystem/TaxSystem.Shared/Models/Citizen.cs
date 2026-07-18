namespace TaxSystem.Shared.Models;

public class Citizen
{
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string cpr { get; set; }
    public string streetAddress { get; set; }
    public string city { get; set; }
    public string zipCode { get; set; }
    public string bankAccountNumber { get; set; }
}