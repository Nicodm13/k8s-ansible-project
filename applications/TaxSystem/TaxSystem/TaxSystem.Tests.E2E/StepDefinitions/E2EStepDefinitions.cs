using System.Net.Http.Json;
using TaxSystem.Shared.Models;

namespace TaxSystem.Tests.E2E.StepDefinitions;

/// <summary>
/// End-to-end step definitions that interact with the full deployed stack
/// via HTTP (the Client API gateway). Requires services running in K8s/Minikube.
/// </summary>
[Binding]
public sealed class E2EStepDefinitions : IDisposable
{
    private readonly HttpClient _httpClient;

    private string? _companyCvr;
    private string? _companyName;
    private string? _employeeName;
    private string? _employeeCpr;
    private int _salary;
    private HttpResponseMessage? _lastResponse;

    public E2EStepDefinitions()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CLIENT_BASE_URL") ?? "http://localhost:8080";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    [Given(@"a company with CVR ""(.*)""")]
    public void GivenACompanyWithCvr(string cvr)
    {
        _companyCvr = cvr;
    }

    [Given(@"a company named ""(.*)"" with CVR ""(.*)""")]
    public void GivenACompanyNamedWithCvr(string name, string cvr)
    {
        _companyName = name;
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
        var year = DateTime.Now.Year;
        _lastResponse = await _httpClient.PostAsJsonAsync(
            $"/Company/{_companyCvr}/employees/income/{year}/{_employeeCpr}", _salary);
    }

    [When(@"the salary report is generated")]
    public async Task WhenTheSalaryReportIsGenerated()
    {
        var year = DateTime.Now.Year;
        _lastResponse = await _httpClient.PostAsJsonAsync(
            $"/Citizen/{_employeeCpr}/income/{year}", _salary);
    }

    [When(@"the company is registered through the client API")]
    public async Task WhenTheCompanyIsRegisteredThroughTheClientApi()
    {
        var company = new Company
        {
            CVR = _companyCvr!,
            Name = _companyName!
        };

        _lastResponse = await _httpClient.PostAsJsonAsync("/Company", company);
    }

    [Then(@"the statement should contain ""(.*)"" and a gross income of (\d+)")]
    public async Task ThenTheStatementShouldContainAndAGrossIncomeOf(string name, int grossIncome)
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

    [Then(@"the company lookup should return ""(.*)"" with CVR ""(.*)""")]
    public async Task ThenTheCompanyLookupShouldReturnWithCvr(string name, string cvr)
    {
        Assert.That(_lastResponse, Is.Not.Null);
        Assert.That(_lastResponse!.IsSuccessStatusCode, Is.True,
            $"Expected company registration to succeed but got {_lastResponse.StatusCode}: {await _lastResponse.Content.ReadAsStringAsync()}");

        Company? company = null;
        string content = string.Empty;
        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            response = await _httpClient.GetAsync($"/Company/{cvr}");
            content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                company = await response.Content.ReadFromJsonAsync<Company>();
                if (company?.Name == name && company.CVR == cvr)
                {
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True,
            $"Expected company lookup to succeed but got {response.StatusCode}: {content}");
        Assert.That(company, Is.Not.Null, $"Expected company response body but got: {content}");
        Assert.That(company!.Name, Is.EqualTo(name));
        Assert.That(company.CVR, Is.EqualTo(cvr));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

