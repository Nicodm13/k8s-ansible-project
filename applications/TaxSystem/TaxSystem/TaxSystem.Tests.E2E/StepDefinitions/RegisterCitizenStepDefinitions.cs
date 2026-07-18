using System.Net.Http.Json;
using System.Text.Json;
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
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
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

    [Then(@"the response body should contain CPR ""(.*)""")]
    public async Task ThenTheResponseBodyShouldContainCpr(string expectedCpr)
    {
        var content = await _response!.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain(expectedCpr),
            $"Expected response to contain CPR '{expectedCpr}' but got: {content}");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

