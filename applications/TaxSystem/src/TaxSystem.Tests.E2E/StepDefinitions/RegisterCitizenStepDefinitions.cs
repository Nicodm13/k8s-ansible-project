using System.Net.Http.Json;
using TaxSystem.Shared.Models;

namespace TaxSystem.Tests.E2E.StepDefinitions;

[Binding]
public sealed class RegisterCitizenStepDefinitions : IDisposable
{
    private readonly HttpClient _httpClient;
    private Citizen? _citizen;
    private HttpResponseMessage? _response;

    public RegisterCitizenStepDefinitions()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CLIENT_BASE_URL") ?? "http://localhost:8080";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [Given(@"a citizen with CPR ""(.*)"" named ""(.*)"" ""(.*)"" living at ""(.*)"", ""(.*)"", ""(.*)"" with bank account ""(.*)""")]
    public void GivenACitizenWithDetails(string cpr, string firstName, string lastName, string street, string city, string zip, string bankAccount)
    {
        _citizen = new Citizen
        {
            cpr = cpr,
            firstName = firstName,
            lastName = lastName,
            streetAddress = street,
            city = city,
            zipCode = zip,
            bankAccountNumber = bankAccount
        };
    }

    [When(@"the citizen registration is submitted")]
    public async Task WhenTheCitizenRegistrationIsSubmitted()
    {
        _response = await _httpClient.PostAsJsonAsync("/Citizen", _citizen);
    }

    [Then(@"the response status code should be 200")]
    public void ThenTheResponseStatusCodeShouldBe200()
    {
        Assert.That((int)_response!.StatusCode, Is.EqualTo(200),
            $"Expected 200 but got {(int)_response.StatusCode}");
    }

    [Then(@"the citizen lookup should return ""(.*)"" ""(.*)"" with CPR ""(.*)""")]
    public async Task ThenTheCitizenLookupShouldReturnWithCpr(string firstName, string lastName, string cpr)
    {
        Citizen? citizen = null;
        string content = string.Empty;
        HttpResponseMessage? response = null;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            response = await _httpClient.GetAsync($"/Citizen/{cpr}");
            content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                citizen = await response.Content.ReadFromJsonAsync<Citizen>();
                if (citizen?.cpr == cpr && citizen.firstName == firstName && citizen.lastName == lastName)
                {
                    break;
                }
            }

            // await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.IsSuccessStatusCode, Is.True,
            $"Expected citizen lookup to succeed but got {response.StatusCode}: {content}");
        Assert.That(citizen, Is.Not.Null, $"Expected citizen response body but got: {content}");
        Assert.That(citizen!.cpr, Is.EqualTo(cpr));
        Assert.That(citizen.firstName, Is.EqualTo(firstName));
        Assert.That(citizen.lastName, Is.EqualTo(lastName));
    }

    public void Dispose()
    {
        if (!string.IsNullOrWhiteSpace(_citizen?.cpr))
        {
            try
            {
                _httpClient.DeleteAsync($"/Citizen/{_citizen.cpr}").GetAwaiter().GetResult();
            }
            catch
            {
                // Best-effort cleanup must not hide the scenario result.
            }
        }

        _httpClient.Dispose();
    }
}

