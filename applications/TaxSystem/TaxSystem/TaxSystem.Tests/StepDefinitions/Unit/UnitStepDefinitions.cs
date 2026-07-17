using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Models;
namespace TaxSystem.Tests.StepDefinitions.Unit;
/// <summary>
/// Unit-level step definitions that test services in isolation using the in-memory
/// synchronous message queue. No external infrastructure required.
/// </summary>
[Binding]
[Scope(Tag = "unit")]
public sealed class UnitStepDefinitions
{
    private readonly MessageQueueSync _messageQueue = new();
    private string? _companyCvr;
    private string? _employeeName;
    private string? _employeeCpr;
    private int _salary;
    private Event? _lastPublishedEvent;
    [Given(@"a company with CVR ""(.*)""")]
    public void GivenACompanyWithCvr(string cvr)
    {
        _companyCvr = cvr;
    }
    [Given(@"an employee named ""(.*)"" with CPR ""(.*)""")]
    public void GivenAnEmployeeNamedWithCpr(string name, string cpr)
    {
        _employeeName = name;
        _employeeCpr = cpr;
    }
    [Given(@"the employee's annual salary is reported as (\d+)")]
    public void GivenTheEmployeesAnnualSalaryIsReportedAs(int salary)
    {
        _salary = salary;
    }
    [When(@"the statement is generated")]
    public void WhenTheStatementIsGenerated()
    {
        _messageQueue.AddHandler("SalaryReported", e => _lastPublishedEvent = e);
        _messageQueue.Publish(new Event("SalaryReported", _employeeCpr, _employeeName, _salary));
    }
    [When(@"the salary report is generated")]
    public void WhenTheSalaryReportIsGenerated()
    {
        _messageQueue.AddHandler("SalaryReported", e => _lastPublishedEvent = e);
        _messageQueue.Publish(new Event("SalaryReported", _employeeCpr, _employeeName, _salary));
    }
    [Then(@"the statement should contain ""(.*)"" and a gross income of (\d+)")]
    public void ThenTheStatementShouldContainAndAGrossIncomeOf(string name, int grossIncome)
    {
        Assert.That(_lastPublishedEvent, Is.Not.Null, "Expected a SalaryReported event to be published.");
        Assert.That(_lastPublishedEvent!.GetArgument<string>(1), Is.EqualTo(name));
        Assert.That(_lastPublishedEvent.GetArgument<int>(2), Is.EqualTo(grossIncome));
    }
    [Then(@"the report should contain ""(.*)"" and a gross income of (\d+)")]
    public void ThenTheReportShouldContainAndAGrossIncomeOf(string name, int grossIncome)
    {
        Assert.That(_lastPublishedEvent, Is.Not.Null, "Expected a SalaryReported event to be published.");
        Assert.That(_lastPublishedEvent!.GetArgument<string>(1), Is.EqualTo(name));
        Assert.That(_lastPublishedEvent.GetArgument<int>(2), Is.EqualTo(grossIncome));
    }
}
