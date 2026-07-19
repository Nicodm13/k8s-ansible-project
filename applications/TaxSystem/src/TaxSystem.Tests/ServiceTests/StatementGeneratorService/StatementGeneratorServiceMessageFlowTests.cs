using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Persistance;
using TaxSystem.StatementGenerator.Consumers;
using TaxSystem.StatementGenerator.Repositories;
using BackendStatementGeneratorService = TaxSystem.StatementGenerator.Services.StatementGeneratorService;

namespace TaxSystem.Tests.ServiceTests.StatementGeneratorService;

public class StatementGeneratorServiceMessageFlowTests
{
    private string _dataPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _dataPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "statement-generator-service-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dataPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dataPath))
        {
            Directory.Delete(_dataPath, recursive: true);
        }
    }

    [Test]
    public async Task TaxInfoReportedPersistsStatementAndPublishesGenerateTaxStatement()
    {
        var generateTaxStatement = new TaskCompletionSource<GenerateTaxStatement>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("generate-tax-statement-test", endpoint =>
            {
                endpoint.Handler<GenerateTaxStatement>(context =>
                {
                    generateTaxStatement.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            await bus.Publish(new TaxInfoReported("101011234", "John Doe", 100000m, 5000m, 15000m, 20000m));

            var message = await generateTaxStatement.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var statement = await provider.GetRequiredService<IReadStatementRepository>().GetByCprAsync("101011234");

            Assert.That(message, Is.EqualTo(new GenerateTaxStatement("101011234")));
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement!.name, Is.EqualTo("John Doe"));
            Assert.That(statement.annualGrossSalary, Is.EqualTo("100000"));
            Assert.That(statement.annualCapitalGains, Is.EqualTo("5000"));
            Assert.That(statement.annualTotalDeduction, Is.EqualTo("15000"));
            Assert.That(statement.annualPaidTax, Is.EqualTo("20000"));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task GenerateTaxStatementPublishesStatementGeneratedWhenCitizenAndTaxInfoExist()
    {
        var statementGenerated = new TaskCompletionSource<StatementGenerated>();
        var scheduleBankTransfer = new TaskCompletionSource<ScheduleBankTransfer>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("statement-generated-test", endpoint =>
            {
                endpoint.Handler<StatementGenerated>(context =>
                {
                    statementGenerated.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });

            configurator.ReceiveEndpoint("schedule-bank-transfer-test", endpoint =>
            {
                endpoint.Handler<ScheduleBankTransfer>(context =>
                {
                    scheduleBankTransfer.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();
        await provider.GetRequiredService<IWriteStatementRepository>().SaveAsync("101011234", new TaxSystem.Shared.Models.Statement
        {
            name = string.Empty,
            annualGrossSalary = "100000",
            annualCapitalGains = "0",
            annualTotalDeduction = "0",
            annualPaidTax = "50000"
        });

        await bus.StartAsync();
        try
        {
            await bus.Publish(new GenerateTaxStatement("101011234"));

            var generated = await statementGenerated.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var transfer = await scheduleBankTransfer.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var statement = await provider.GetRequiredService<IReadStatementRepository>().GetByCprAsync("101011234");

            Assert.That(generated, Is.EqualTo(new StatementGenerated("101011234", "John Doe", 100000m, 0m, 0m, 50000m, 37000m, -13000m)));
            Assert.That(transfer, Is.EqualTo(new ScheduleBankTransfer("101011234", 13000m, "1234567890", string.Empty)));
            Assert.That(statement, Is.Not.Null);
            Assert.That(statement!.name, Is.EqualTo("John Doe"));
            Assert.That(statement.annualTax, Is.EqualTo("37000"));
            Assert.That(statement.annualOwedTax, Is.EqualTo("-13000"));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task GenerateTaxStatementPublishesStatementNotReadyWhenTaxInfoIsMissing()
    {
        var statementNotReady = new TaskCompletionSource<StatementNotReady>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("statement-not-ready-test", endpoint =>
            {
                endpoint.Handler<StatementNotReady>(context =>
                {
                    statementNotReady.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            await bus.Publish(new GenerateTaxStatement("101011234"));

            var message = await statementNotReady.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.That(message, Is.EqualTo(new StatementNotReady("101011234", "Tax info has not been reported")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private ServiceProvider BuildProvider(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => new FileSystemRepository("statements", _dataPath));
        services.AddSingleton<StatementRepository>();
        services.AddSingleton<IReadStatementRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<StatementRepository>());
        services.AddSingleton<IWriteStatementRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<StatementRepository>());
        services.AddSingleton<BackendStatementGeneratorService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<TaxInfoReportedConsumer>();
            busRegistrationConfigurator.AddConsumer<GenerateTaxStatementConsumer>();
            busRegistrationConfigurator.UsingInMemory((context, configurator) =>
            {
                configurator.ReceiveEndpoint("statement-generator-service", endpoint =>
                {
                    endpoint.ConfigureConsumer<TaxInfoReportedConsumer>(context);
                    endpoint.ConfigureConsumer<GenerateTaxStatementConsumer>(context);
                    endpoint.Handler<CitizenInfoRequested>(async requestContext =>
                    {
                        if (requestContext.Message.Cpr == "101011234")
                        {
                            await requestContext.RespondAsync(new CitizenInfoReceived(
                                "101011234",
                                "John",
                                "Doe",
                                "Main Street 1",
                                "Copenhagen",
                                "1000",
                                "1234567890"));
                        }
                        else
                        {
                            await requestContext.RespondAsync(new CitizenInfoNotFound(requestContext.Message.Cpr));
                        }
                    });
                });

                configure(configurator);
            });
        });

        return services.BuildServiceProvider(validateScopes: true);
    }
}
