using TaxSystem.Shared.Messaging;

namespace TaxSystem.Tests.StepDefinitions;

/// <summary>
/// Step definitions for the Report Salary feature.
/// Tests the salary reporting flow through the message queue.
/// </summary>
[Binding]
public sealed class ReportSalaryStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly MessageQueueSync _messageQueue = new();
    private Event? _lastPublishedEvent;

    public ReportSalaryStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When(@"the statement is generated")]
    public void WhenTheStatementIsGenerated()
    {
        var cpr = _scenarioContext["EmployeeCpr"] as string;
        var name = _scenarioContext["EmployeeName"] as string;
        var salary = (int)_scenarioContext["Salary"];

        _messageQueue.AddHandler("SalaryReported", e => _lastPublishedEvent = e);
        _messageQueue.Publish(new Event("SalaryReported", cpr, name, salary));
    }

    [Then(@"the statement should contain ""(.*)"" and a gross income of (\d+)")]
    public void ThenTheStatementShouldContainAndAGrossIncomeOf(string name, int grossIncome)
    {
        Assert.That(_lastPublishedEvent, Is.Not.Null, "Expected a SalaryReported event to be published.");
        Assert.That(_lastPublishedEvent!.GetArgument<string>(1), Is.EqualTo(name));
        Assert.That(_lastPublishedEvent.GetArgument<int>(2), Is.EqualTo(grossIncome));
    }
}

