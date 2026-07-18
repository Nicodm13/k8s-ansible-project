using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;

namespace TaxSystem.Tests.StepDefinitions;

/// <summary>
/// Step definitions for the Request Statement feature.
/// Tests the statement request flow through the message queue.
/// </summary>
[Binding]
public sealed class RequestStatementStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private SalaryReported? _lastPublishedEvent;

    public RequestStatementStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [When(@"the salary report is generated")]
    public async Task WhenTheSalaryReportIsGenerated()
    {
        var cpr = _scenarioContext["EmployeeCpr"] as string
            ?? throw new InvalidOperationException("Employee CPR is missing from the scenario context.");
        var name = _scenarioContext["EmployeeName"] as string
            ?? throw new InvalidOperationException("Employee name is missing from the scenario context.");
        var salary = (int)_scenarioContext["Salary"];
        var received = new TaskCompletionSource<SalaryReported>();
        var bus = Bus.Factory.CreateUsingInMemory(configurator =>
        {
            configurator.ReceiveEndpoint("salary-reported-request-test", endpoint =>
            {
                endpoint.Handler<SalaryReported>(context =>
                {
                    received.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });

        await bus.StartAsync();
        try
        {
            await bus.Publish(new SalaryReported(cpr, name, salary));
            _lastPublishedEvent = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Then(@"the report should contain ""(.*)"" and a gross income of (\d+)")]
    public void ThenTheReportShouldContainAndAGrossIncomeOf(string name, int grossIncome)
    {
        Assert.That(_lastPublishedEvent, Is.Not.Null, "Expected a SalaryReported event to be published.");
        Assert.That(_lastPublishedEvent!.Name, Is.EqualTo(name));
        Assert.That(_lastPublishedEvent.Salary, Is.EqualTo(grossIncome));
    }
}

