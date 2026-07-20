using TaxSystem.Shared.Messaging;

namespace TaxSystem.Tests.StepDefinitions;

/// <summary>
/// Shared step definitions used across multiple feature files.
/// Contains common Given steps for setting up test context.
/// </summary>
[Binding]
public sealed class SharedStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;

    public SharedStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"a company with CVR ""(.*)""")]
    public void GivenACompanyWithCvr(string cvr)
    {
        _scenarioContext["CompanyCvr"] = cvr;
    }

    [Given(@"an employee named ""(.*)"" with CPR ""(.*)""")]
    public void GivenAnEmployeeNamedWithCpr(string name, string cpr)
    {
        _scenarioContext["EmployeeName"] = name;
        _scenarioContext["EmployeeCpr"] = cpr;
        if (!_scenarioContext.ContainsKey("GivenOrder"))
            _scenarioContext["GivenOrder"] = new List<string>();
    }

    [Given(@"the employee's annual salary is reported as (\d+) with a paid tax of (\d+)")]
    public void GivenTheEmployeesAnnualSalaryIsReportedAs(int salary, int paidTax)
    {
        _scenarioContext["Salary"] = salary;
        _scenarioContext["PaidTax"] = paidTax;
        var order = (List<string>)_scenarioContext["GivenOrder"];
        order.Add("Salary");
    }
}

