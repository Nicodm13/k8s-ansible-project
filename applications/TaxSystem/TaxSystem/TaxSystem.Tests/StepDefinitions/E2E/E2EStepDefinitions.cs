using System.Net.Http.Json;
using TaxSystem.Shared.Models;

namespace TaxSystem.Tests.StepDefinitions.E2E;

/// <summary>
/// End-to-end step definitions that interact with the full deployed stack
/// via HTTP (the Client API gateway). Requires services running in K8s/Minikube.
/// </summary>
[Binding]
[Scope(Tag = "e2e")]
public sealed class E2EStepDefinitions : IDisposable
{
    private readonly HttpClient _httpClient;

    private string? _companyCvr;
    private string? _employeeName;
    private string? _employeeCpr;
    private int _salary;
    private HttpResponseMessage? _lastResponse;

    public E2EStepDefinitions()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CLIENT_BASE_URL") ?? "http://localhost:8080";
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

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
    public async Task WhenTheStatementIsGenerated()
    {
        // Report salary via Company endpoint
        var year = DateTime.Now.Year;
        _lastResponse = await _httpClient.PostAsJsonAsync(
            $"/Company/{_companyCvr}/employees/income/{year}/{_employeeCpr}", _salary);
    }

    [When(@"the salary report is generated")]
    public async Task WhenTheSalaryReportIsGenerated()
    {
        var year = DateTime.Now.Year;
        // Report income via Citizen endpoint
        _lastResponse = await _httpClient.PostAsJsonAsync(
            $"/Citizen/{_employeeCpr}/income/{year}", _salary);
    }

    [Then(@"the statement should contain ""(.*)"" and a gross income of (\d+)")]
    public async Task ThenTheStatementShouldContainAndAGrossIncomeOf(string name, int grossIncome)
    {
        // Wait briefly for async processing across services
        await Task.Delay(TimeSpan.FromSeconds(2));

        var year = DateTime.Now.Year;
        var response = await _httpClient.GetAsync($"/Citizen/{_employeeCpr}/Statements/{year}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Expected success but got {response.StatusCode}: {content}");
        Assert.That(content, Does.Contain(name));
        Assert.That(content, Does.Contain(grossIncome.ToString()));
    }

    [Then(@"the report should contain ""(.*)"" and a gross income of (\d+)")]
    public async Task ThenTheReportShouldContainAndAGrossIncomeOf(string name, int grossIncome)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        var year = DateTime.Now.Year;
        var response = await _httpClient.GetAsync($"/Citizen/{_employeeCpr}/Statements/{year}");
        var content = await response.Content.ReadAsStringAsync();

        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Expected success but got {response.StatusCode}: {content}");
        Assert.That(content, Does.Contain(name));
        Assert.That(content, Does.Contain(grossIncome.ToString()));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

