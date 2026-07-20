using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.CompanyService.Consumers;
using TaxSystem.CompanyService.Persistance;
using TaxSystem.CompanyService.Repositories;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;
using BackendCompanyService = TaxSystem.CompanyService.Services.CompanyService;

namespace TaxSystem.Tests.ServiceTests.CompanyService;

public class CompanyServiceMessageFlowTests
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
    public async Task CompanyRegistrationRequestPersistsCompanyAndPublishesRegisteredEvent()
    {
        var registered = new TaskCompletionSource<CompanyRegistered>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("company-registered-test", endpoint =>
            {
                endpoint.Handler<CompanyRegistered>(context =>
                {
                    registered.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();
        await bus.StartAsync();
        try
        {
            await bus.Publish(new CompanyRegistrationRequested("12345678", "Acme Corp"));

            var message = await registered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var scope = provider.CreateScope();
            var company = await scope.ServiceProvider.GetRequiredService<IReadCompanyRepository>().GetByCvrAsync("12345678");

            Assert.That(message, Is.EqualTo(new CompanyRegistered("12345678", "Acme Corp")));
            Assert.That(company, Is.Not.Null);
            Assert.That(company!.CVR, Is.EqualTo("12345678"));
            Assert.That(company.Name, Is.EqualTo("Acme Corp"));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task CompanyRegistrationRequestRespondsAfterCompanyIsPersisted()
    {
        await using var provider = BuildProvider(_ => { });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<CompanyRegistrationRequested>>();
            var response = await client.GetResponse<CompanyRegistered>(new CompanyRegistrationRequested("12345678", "Acme Corp"));

            using var readScope = provider.CreateScope();
            var company = await readScope.ServiceProvider.GetRequiredService<IReadCompanyRepository>().GetByCvrAsync("12345678");

            Assert.That(response.Message, Is.EqualTo(new CompanyRegistered("12345678", "Acme Corp")));
            Assert.That(company, Is.Not.Null);
            Assert.That(company!.CVR, Is.EqualTo("12345678"));
            Assert.That(company.Name, Is.EqualTo("Acme Corp"));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task ReportSalaryPublishesCompanyAndTaxInfoWhenCompanyExists()
    {
        var companyInfoReceived = new TaskCompletionSource<CompanyInfoReceived>();
        var taxInfoReported = new TaskCompletionSource<TaxInfoReported>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("company-info-received-test", endpoint =>
            {
                endpoint.Handler<CompanyInfoReceived>(context =>
                {
                    companyInfoReceived.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });

            configurator.ReceiveEndpoint("tax-info-reported-test", endpoint =>
            {
                endpoint.Handler<TaxInfoReported>(context =>
                {
                    taxInfoReported.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();
        using (var setupScope = provider.CreateScope())
        {
            await setupScope.ServiceProvider.GetRequiredService<IWriteCompanyRepository>().SaveAsync(new Company
            {
                CVR = "12345678",
                Name = "Acme Corp"
            });
        }

        await bus.StartAsync();
        try
        {
            await bus.Publish(new ReportSalary("12345678", 2026, "0101011234", 100000m));

            var companyInfo = await companyInfoReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var taxInfo = await taxInfoReported.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.That(companyInfo, Is.EqualTo(new CompanyInfoReceived("12345678", "Acme Corp")));
            Assert.That(taxInfo.Cpr, Is.EqualTo("0101011234"));
            Assert.That(taxInfo.AnnualGrossSalary, Is.EqualTo(100000m));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task CompanyInfoRequestReturnsPersistedCompany()
    {
        await using var provider = BuildProvider(_ => { });
        var bus = provider.GetRequiredService<IBusControl>();
        using (var setupScope = provider.CreateScope())
        {
            await setupScope.ServiceProvider.GetRequiredService<IWriteCompanyRepository>().SaveAsync(new Company
            {
                CVR = "12345678",
                Name = "Acme Corp"
            });
        }

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<CompanyInfoRequested>>();
            var response = await client.GetResponse<CompanyInfoReceived>(new CompanyInfoRequested("12345678"));

            Assert.That(response.Message, Is.EqualTo(new CompanyInfoReceived("12345678", "Acme Corp")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task CompanyInfoRequestReturnsNotFoundWhenCompanyDoesNotExist()
    {
        await using var provider = BuildProvider(_ => { });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<CompanyInfoRequested>>();
            var response = await client.GetResponse<CompanyInfoNotFound>(new CompanyInfoRequested("99999999"));

            Assert.That(response.Message, Is.EqualTo(new CompanyInfoNotFound("99999999")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task CompanyDeregistrationRequestDeletesCompanyAndPublishesDeregisteredEvent()
    {
        var deregistered = new TaskCompletionSource<CompanyDeregistered>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("company-deregistered-test", endpoint =>
            {
                endpoint.Handler<CompanyDeregistered>(context =>
                {
                    deregistered.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();
        using (var setupScope = provider.CreateScope())
        {
            await setupScope.ServiceProvider.GetRequiredService<IWriteCompanyRepository>().SaveAsync(new Company
            {
                CVR = "12345678",
                Name = "Acme Corp"
            });
        }

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<CompanyDeregistrationRequested>>();
            var response = await client.GetResponse<CompanyDeregistered>(new CompanyDeregistrationRequested("12345678"));
            var message = await deregistered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var readScope = provider.CreateScope();
            var company = await readScope.ServiceProvider.GetRequiredService<IReadCompanyRepository>().GetByCvrAsync("12345678");

            Assert.That(response.Message, Is.EqualTo(new CompanyDeregistered("12345678")));
            Assert.That(message, Is.EqualTo(new CompanyDeregistered("12345678")));
            Assert.That(company, Is.Null);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private ServiceProvider BuildProvider(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var services = new ServiceCollection();
        services.AddDbContext<CompanyDbContext>(options =>
        {
            options.UseSqlite(_sqliteConnection);
        });
        services.AddScoped<IReadCompanyRepository, CompanyPostgresRepository>();
        services.AddScoped<IWriteCompanyRepository, CompanyPostgresRepository>();
        services.AddScoped<BackendCompanyService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<CompanyRegistrationRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<CompanyUpdateRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<CompanyDeregistrationRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<CompanyInfoRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<ReportSalaryConsumer>();
            busRegistrationConfigurator.AddRequestClient<CompanyRegistrationRequested>();
            busRegistrationConfigurator.AddRequestClient<CompanyDeregistrationRequested>();
            busRegistrationConfigurator.AddRequestClient<CompanyInfoRequested>();
            busRegistrationConfigurator.UsingInMemory((context, configurator) =>
            {
                configurator.ReceiveEndpoint("company-service", endpoint =>
                {
                    endpoint.ConfigureConsumer<CompanyRegistrationRequestedConsumer>(context);
                    endpoint.ConfigureConsumer<CompanyUpdateRequestedConsumer>(context);
                    endpoint.ConfigureConsumer<CompanyDeregistrationRequestedConsumer>(context);
                    endpoint.ConfigureConsumer<CompanyInfoRequestedConsumer>(context);
                    endpoint.ConfigureConsumer<ReportSalaryConsumer>(context);
                });

                configure(configurator);
            });
        });

        var provider = services.BuildServiceProvider(validateScopes: true);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }
}
