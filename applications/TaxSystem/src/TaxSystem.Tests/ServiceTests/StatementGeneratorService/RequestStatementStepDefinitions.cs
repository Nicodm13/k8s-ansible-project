using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Persistance;
using TaxSystem.StatementGenerator.Consumers;
using TaxSystem.StatementGenerator.Repositories;
using BackendStatementGeneratorService = TaxSystem.StatementGenerator.Services.StatementGeneratorService;

namespace TaxSystem.Tests.StepDefinitions;

/// <summary>
/// Step definitions for the Request Statement feature.
/// Tests the statement request flow through the StatementGeneratorService using MassTransit in-memory transport.
/// </summary>
[Binding]
public sealed class RequestStatementStepDefinitions : IDisposable
{
    private readonly ScenarioContext _scenarioContext;
    private StatementGenerated? _statementGenerated;
    private StatementNotReady? _statementNotReady;
    private string _dataPath = string.Empty;

    public RequestStatementStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"the employee reports deductibles of (\d+)")]
    public void GivenTheEmployeeReportsDeductiblesOf(int deductibles)
    {
        _scenarioContext["Deductibles"] = deductibles;
        var order = (List<string>)_scenarioContext["GivenOrder"];
        order.Add("Deductibles");
    }

    [When(@"the salary report is generated")]
    public async Task WhenTheSalaryReportIsGenerated()
    {
        var cpr = _scenarioContext["EmployeeCpr"] as string
            ?? throw new InvalidOperationException("Employee CPR is missing from the scenario context.");
        var name = _scenarioContext["EmployeeName"] as string
            ?? throw new InvalidOperationException("Employee name is missing from the scenario context.");

        _dataPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "request-statement-bdd", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataPath);

        var statementGenerated = new TaskCompletionSource<StatementGenerated>();
        var statementNotReady = new TaskCompletionSource<StatementNotReady>();

        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("statement-generated-bdd-test", endpoint =>
            {
                endpoint.Handler<StatementGenerated>(context =>
                {
                    statementGenerated.TrySetResult(context.Message);
                    return Task.CompletedTask;
                });
            });

            configurator.ReceiveEndpoint("statement-not-ready-bdd-test", endpoint =>
            {
                endpoint.Handler<StatementNotReady>(context =>
                {
                    statementNotReady.TrySetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        }, cpr, name);

        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            // Submit deductibles report if the citizen reported them BEFORE salary
            var hasDeductibles = _scenarioContext.ContainsKey("Deductibles");
            var hasSalary = _scenarioContext.ContainsKey("Salary");

            if (hasDeductibles && hasSalary)
            {
                // Both present: check order. If deductibles were Given before salary,
                // submit deductibles first then salary (scenario 3).
                // If salary was Given before deductibles, only submit deductibles (scenario 2 - citizen only flow).
                var givenOrder = (List<string>)_scenarioContext["GivenOrder"];
                var deductiblesFirst = givenOrder.IndexOf("Deductibles") < givenOrder.IndexOf("Salary");

                if (deductiblesFirst)
                {
                    // Scenario 3: citizen reported deductibles, then company reported salary
                    var deductibles = (int)_scenarioContext["Deductibles"];
                    await bus.Publish(new TaxInfoReported(cpr, name, null, null, deductibles, null));
                    await Task.Delay(20);
                    var salary = (int)_scenarioContext["Salary"];
                    var paidTax = (int)_scenarioContext["PaidTax"];
                    await bus.Publish(new TaxInfoReported(cpr, name, salary, null, null, paidTax));
                }
                else
                {
                    // Scenario 2: company reported salary but citizen only submitted deductibles to service
                    // The salary hasn't been reported to the system yet - only deductibles
                    var deductibles = (int)_scenarioContext["Deductibles"];
                    await bus.Publish(new TaxInfoReported(cpr, null, null, null, deductibles, null));
                }
            }
            else if (hasSalary)
            {
                // Scenario 1: only salary reported
                var salary = (int)_scenarioContext["Salary"];
                var paidTax = (int)_scenarioContext["PaidTax"];
                await bus.Publish(new TaxInfoReported(cpr, name, salary, null, null, paidTax));
            }
            else if (hasDeductibles)
            {
                // Only deductibles reported
                var deductibles = (int)_scenarioContext["Deductibles"];
                await bus.Publish(new TaxInfoReported(cpr, null, null, null, deductibles, null));
            }

            // Wait for whichever result arrives
            var completedTask = await Task.WhenAny(
                statementGenerated.Task,
                statementNotReady.Task,
                Task.Delay(TimeSpan.FromSeconds(5)));

            if (statementGenerated.Task.IsCompletedSuccessfully)
                _statementGenerated = statementGenerated.Task.Result;
            else if (statementNotReady.Task.IsCompletedSuccessfully)
                _statementNotReady = statementNotReady.Task.Result;
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Then(@"the report should contain ""(.*)"" and a gross income of (\d+)")]
    public void ThenTheReportShouldContainAndAGrossIncomeOf(string name, int grossIncome)
    {
        Assert.That(_statementGenerated, Is.Not.Null, "Expected a StatementGenerated message but none was received.");
        Assert.That(_statementGenerated!.Name, Is.EqualTo(name));
        Assert.That(_statementGenerated.AnnualGrossSalary, Is.EqualTo((decimal)grossIncome));
    }

    [Then(@"No report should be generated")]
    public void ThenNoReportShouldBeGenerated()
    {
        Assert.That(_statementGenerated, Is.Null, "Expected no StatementGenerated message but one was received.");
        Assert.That(_statementNotReady, Is.Not.Null, "Expected a StatementNotReady message but none was received.");
    }

    [Then(@"A message should be sent containing ""(.*)""")]
    public void ThenAMessageShouldBeSentContaining(string expectedFragment)
    {
        Assert.That(_statementNotReady, Is.Not.Null, "Expected a StatementNotReady message.");
        Assert.That(_statementNotReady!.Reason, Does.Contain(expectedFragment).IgnoreCase);
    }

    [Then(@"the report should contain ""(.*)"" and a deductible of (\d+)")]
    public void ThenTheReportShouldContainAndADeductibleOf(string name, int deductible)
    {
        Assert.That(_statementGenerated, Is.Not.Null, "Expected a StatementGenerated message.");
        Assert.That(_statementGenerated!.Name, Is.EqualTo(name));
        Assert.That(_statementGenerated.AnnualTotalDeduction, Is.EqualTo((decimal)deductible));
    }

    public void Dispose()
    {
        if (!string.IsNullOrEmpty(_dataPath) && Directory.Exists(_dataPath))
        {
            Directory.Delete(_dataPath, recursive: true);
        }
    }

    private ServiceProvider BuildProvider(
        Action<IInMemoryBusFactoryConfigurator> configure,
        string cpr,
        string name)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => new FileSystemRepository("statements", _dataPath));
        services.AddSingleton<StatementRepository>();
        services.AddSingleton<IReadStatementRepository>(sp => sp.GetRequiredService<StatementRepository>());
        services.AddSingleton<IWriteStatementRepository>(sp => sp.GetRequiredService<StatementRepository>());
        services.AddSingleton<BackendStatementGeneratorService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<TaxInfoReportedConsumer>();
            busRegistrationConfigurator.AddConsumer<GenerateTaxStatementConsumer>();
            busRegistrationConfigurator.UsingInMemory((context, configurator) =>
            {
                configurator.ReceiveEndpoint("statement-generator-service-bdd", endpoint =>
                {
                    endpoint.ConfigureConsumer<TaxInfoReportedConsumer>(context);
                    endpoint.ConfigureConsumer<GenerateTaxStatementConsumer>(context);
                    endpoint.Handler<CitizenInfoRequested>(async requestContext =>
                    {
                        // Split name into first/last
                        var parts = name.Split(' ', 2);
                        var firstName = parts[0];
                        var lastName = parts.Length > 1 ? parts[1] : string.Empty;
                        await requestContext.RespondAsync(new CitizenInfoReceived(
                            cpr, firstName, lastName, "Test Street 1", "Copenhagen", "1000", "9999999999"));
                    });
                });

                configure(configurator);
            });
        });

        return services.BuildServiceProvider(validateScopes: true);
    }
}

