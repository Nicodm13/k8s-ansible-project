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
    private long? _paidTax;
    private HttpResponseMessage? _lastResponse;

    public E2EStepDefinitions()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CLIENT_BASE_URL") ?? "http://localhost:8080";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    [Given(@"a company with CVR ""(.*)""")]
    public async Task GivenACompanyWithCvr(string cvr)
    {
        _companyCvr = cvr;

        await _httpClient.PostAsJsonAsync("/Company", new Company
        {
            CVR = cvr,
            Name = $"Company {cvr}"
        });

        await WaitForCompanyLookupAsync(cvr);
    }

    [Given(@"a company named ""(.*)"" with CVR ""(.*)""")]
    public void GivenACompanyNamedWithCvr(string name, string cvr)
    {
        _companyName = name;
        _companyCvr = cvr;
    }

    [Given(@"an employee named ""(.*)"" with CPR ""(.*)""")]
    public async Task GivenAnEmployeeNamedWithCpr(string name, string cpr)
    {
        _employeeName = name;
        _employeeCpr = cpr;

        var names = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        await _httpClient.PostAsJsonAsync("/Citizen", new Citizen
        {
            cpr = cpr,
            firstName = names.ElementAtOrDefault(0) ?? name,
            lastName = names.ElementAtOrDefault(1) ?? string.Empty,
            streetAddress = "Main Street 1",
            city = "Copenhagen",
            zipCode = "1000",
            bankAccountNumber = "1234567890"
        });
    }

    [Given(@"the employee's annual salary is reported as (\d+) and paid tax as (\d+)")]
    public void GivenTheEmployeesAnnualSalaryIsReportedAs(int salary, int paidTax)
    {
        _salary = salary;
        _paidTax = paidTax;
    }

    [When(@"the statement is generated")]
    public async Task WhenTheStatementIsGenerated()
    {
        var year = DateTime.Now.Year;
        _lastResponse = await _httpClient.PostAsJsonAsync(
            $"/Company/{_companyCvr}/employees/income/{year}/{_employeeCpr}",
            new { income = _salary, paidTax = _paidTax });
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
        var year = DateTime.Now.Year;
        var (response, content) = await GetStatementWithRetryAsync(_employeeCpr!, year);

        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Expected success but got {response.StatusCode}: {content}");
        Assert.That(content, Does.Contain(name));
        Assert.That(content, Does.Contain(grossIncome.ToString()));
    }

    [Then(@"the report should contain ""(.*)"" and a gross income of (\d+)")]
    public async Task ThenTheReportShouldContainAndAGrossIncomeOf(string name, int grossIncome)
    {
        var year = DateTime.Now.Year;
        var (response, content) = await GetStatementWithRetryAsync(_employeeCpr!, year);

        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Expected success but got {response.StatusCode}: {content}");
        Assert.That(content, Does.Contain(name));
        Assert.That(content, Does.Contain(grossIncome.ToString()));
    }

    [Then(@"a bank transfer of (\d+) should be scheduled")]
    public async Task ThenABankTransferOfShouldBeScheduled(int amount)
    {
        var (response, content) = await GetBankTransferWithRetryAsync(_employeeCpr!);

        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Expected success but got {response.StatusCode}: {content}");
        Assert.That(content, Does.Contain(_employeeCpr!));
        Assert.That(content, Does.Contain(amount.ToString()));
        Assert.That(content, Does.Contain("Scheduled"));
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
            try
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
            }
            catch (TaskCanceledException exception)
            {
                content = exception.Message;
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

    private async Task WaitForCompanyLookupAsync(string cvr)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await _httpClient.GetAsync($"/Company/{cvr}");
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    private async Task<(HttpResponseMessage Response, string Content)> GetStatementWithRetryAsync(string cpr, int year)
    {
        HttpResponseMessage? response = null;
        string content = string.Empty;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            response = await _httpClient.GetAsync($"/StatementGenerator/{cpr}/Statements/{year}");
            content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return (response!, content);
    }

    private async Task<(HttpResponseMessage Response, string Content)> GetBankTransferWithRetryAsync(string cpr)
    {
        HttpResponseMessage? response = null;
        string content = string.Empty;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            response = await _httpClient.GetAsync($"/Bank/transfers/{cpr}");
            content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return (response!, content);
    }

    public void Dispose()
    {
        if (!string.IsNullOrWhiteSpace(_employeeCpr))
        {
            try
            {
                _httpClient.DeleteAsync($"/Citizen/{_employeeCpr}").GetAwaiter().GetResult();
            }
            catch
            {
                // Best-effort cleanup must not hide the scenario result.
            }
        }

        if (!string.IsNullOrWhiteSpace(_companyCvr))
        {
            try
            {
                _httpClient.DeleteAsync($"/Company/{_companyCvr}").GetAwaiter().GetResult();
            }
            catch
            {
                // Best-effort cleanup must not hide the scenario result.
            }
        }

        _httpClient.Dispose();
    }
}

