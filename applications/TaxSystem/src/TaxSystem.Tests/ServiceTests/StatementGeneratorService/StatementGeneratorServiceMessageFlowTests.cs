using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.StatementGenerator.Consumers;
using TaxSystem.StatementGenerator.Persistance;
using TaxSystem.StatementGenerator.Repositories;
using BackendStatementGeneratorService = TaxSystem.StatementGenerator.Services.StatementGeneratorService;

namespace TaxSystem.Tests.ServiceTests.StatementGeneratorService;

public class StatementGeneratorServiceMessageFlowTests
{
    private SqliteConnection _sqliteConnection = null!;

    [SetUp]
    public void SetUp()
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();
    }

    [TearDown]
    public void TearDown()
    {
        _sqliteConnection.Dispose();
    }

    [Test]
    public async Task TaxInfoReportedPersistsStatementAndPublishesGenerateTaxStatement()
    {
        var generateTaxStatement = new TaskCompletionSource<GenerateTaxStatement>();
        // Only the TaxInfoReportedConsumer is under test here, so GenerateTaxStatementConsumer
        // is intentionally excluded. Otherwise the fanned-out GenerateTaxStatement message would
        // also trigger GenerateTaxStatementConsumer's CitizenInfoRequested request/response, which
        // would still be in-flight when the test tears down the bus, causing StopAsync to block
        // until MassTransit's default 30s request timeout elapses.
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
        }, includeGenerateTaxStatementConsumer: false);
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            await bus.Publish(new TaxInfoReported("101011234", "John Doe", 100000m, 5000m, 15000m, 20000m));

            var message = await generateTaxStatement.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var scope = provider.CreateScope();
            var statement = await scope.ServiceProvider.GetRequiredService<IReadStatementRepository>().GetMergedStatementAsync("101011234");

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
        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IWriteStatementRepository>().SaveReportAsync("101011234", new TaxSystem.Shared.Models.Statement
            {
                cpr = "101011234",
                name = string.Empty,
                annualGrossSalary = "100000",
                annualCapitalGains = "0",
                annualTotalDeduction = "0",
                annualPaidTax = "50000",
                reportedAt = DateTime.UtcNow
            });
        }

        await bus.StartAsync();
        try
        {
            await bus.Publish(new GenerateTaxStatement("101011234"));

            var generated = await statementGenerated.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var transfer = await scheduleBankTransfer.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.That(generated, Is.EqualTo(new StatementGenerated("101011234", "John Doe", 100000m, 0m, 0m, 50000m, 37000m, -13000m)));
            Assert.That(transfer, Is.EqualTo(new ScheduleBankTransfer("101011234", 13000m, "1234567890", string.Empty)));
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

    [Test]
    public async Task IncrementalReportingMergesFieldsFromMultipleReports()
    {
        // We expect two StatementGenerated messages (one per TaxInfoReported).
        var generatedMessages = new List<StatementGenerated>();
        var secondGenerated = new TaskCompletionSource<StatementGenerated>();
        var firstGenerated = new TaskCompletionSource<StatementGenerated>();

        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("statement-generated-incremental-test", endpoint =>
            {
                endpoint.Handler<StatementGenerated>(context =>
                {
                    lock (generatedMessages)
                    {
                        generatedMessages.Add(context.Message);
                        if (generatedMessages.Count == 1)
                            firstGenerated.TrySetResult(context.Message);
                        else if (generatedMessages.Count == 2)
                            secondGenerated.TrySetResult(context.Message);
                    }

                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            // 1. Company reports salary and paid tax only
            await bus.Publish(new TaxInfoReported("101011234", "John Doe", 100000m, null, null, 35000m));

            var first = await firstGenerated.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Null fields treated as 0: tax = max(0, (100000 + 0 - 0) * 0.37) = 37000, owed = 37000 - 35000 = 2000
            Assert.That(first.Cpr, Is.EqualTo("101011234"));
            Assert.That(first.AnnualGrossSalary, Is.EqualTo(100000m));
            Assert.That(first.AnnualCapitalGains, Is.EqualTo(0m));
            Assert.That(first.AnnualTotalDeduction, Is.EqualTo(0m));
            Assert.That(first.AnnualPaidTax, Is.EqualTo(35000m));
            Assert.That(first.AnnualTax, Is.EqualTo(37000m));
            Assert.That(first.AnnualOwedTax, Is.EqualTo(2000m));

            // Small delay to ensure the first report's timestamp is strictly before the second
            await Task.Delay(50);

            // 2. Citizen reports capital gains=5000 and deductions=6000
            await bus.Publish(new TaxInfoReported("101011234", null, null, 5000m, 6000m, null));

            var second = await secondGenerated.Task.WaitAsync(TimeSpan.FromSeconds(5));

            // Merge: newest report has capitalGains=5000, deduction=6000
            // Older report fills in: grossSalary=100000, paidTax=35000
            // tax = max(0, (100000 + 5000 - 6000) * 0.37) = 36630, owed = 36630 - 35000 = 1630
            Assert.That(second.Cpr, Is.EqualTo("101011234"));
            Assert.That(second.AnnualGrossSalary, Is.EqualTo(100000m));
            Assert.That(second.AnnualCapitalGains, Is.EqualTo(5000m));
            Assert.That(second.AnnualTotalDeduction, Is.EqualTo(6000m));
            Assert.That(second.AnnualPaidTax, Is.EqualTo(35000m));
            Assert.That(second.AnnualTax, Is.EqualTo(36630m));
            Assert.That(second.AnnualOwedTax, Is.EqualTo(1630m));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private ServiceProvider BuildProvider(
        Action<IInMemoryBusFactoryConfigurator> configure,
        bool includeGenerateTaxStatementConsumer = true)
    {
        var services = new ServiceCollection();
        services.AddDbContext<StatementDbContext>(options =>
        {
            options.UseSqlite(_sqliteConnection);
        });
        services.AddScoped<IReadStatementRepository, StatementPostgresRepository>();
        services.AddScoped<IWriteStatementRepository, StatementPostgresRepository>();
        services.AddScoped<BackendStatementGeneratorService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<TaxInfoReportedConsumer>();
            if (includeGenerateTaxStatementConsumer)
            {
                busRegistrationConfigurator.AddConsumer<GenerateTaxStatementConsumer>();
            }

            busRegistrationConfigurator.UsingInMemory((context, configurator) =>
            {
                configurator.ReceiveEndpoint("statement-generator-service", endpoint =>
                {
                    endpoint.ConfigureConsumer<TaxInfoReportedConsumer>(context);
                    if (includeGenerateTaxStatementConsumer)
                    {
                        endpoint.ConfigureConsumer<GenerateTaxStatementConsumer>(context);
                    }

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

        var provider = services.BuildServiceProvider(validateScopes: true);

        // Ensure schema is created
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StatementDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }
}
