using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.CompanyService.Consumers;
using TaxSystem.CompanyService.Repositories;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;
using BackendCompanyService = TaxSystem.CompanyService.Services.CompanyService;

namespace TaxSystem.Tests.ServiceTests.CompanyService;

public class CompanyServiceMessageFlowTests
{
    private string _dataPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _dataPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "company-service-tests", Guid.NewGuid().ToString("N"));
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
            var company = await provider.GetRequiredService<IReadCompanyRepository>().GetByCvrAsync("12345678");

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
            var company = await provider.GetRequiredService<IReadCompanyRepository>().GetByCvrAsync("12345678");

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
        await provider.GetRequiredService<IWriteCompanyRepository>().SaveAsync(new Company
        {
            CVR = "12345678",
            Name = "Acme Corp"
        });

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
        await provider.GetRequiredService<IWriteCompanyRepository>().SaveAsync(new Company
        {
            CVR = "12345678",
            Name = "Acme Corp"
        });

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

    private ServiceProvider BuildProvider(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => new FileSystemRepository("companies", _dataPath));
        services.AddSingleton<CompanyRepository>();
        services.AddSingleton<IReadCompanyRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<CompanyRepository>());
        services.AddSingleton<IWriteCompanyRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<CompanyRepository>());
        services.AddSingleton<BackendCompanyService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<CompanyRegistrationRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<CompanyUpdateRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<CompanyDeregistrationRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<CompanyInfoRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<ReportSalaryConsumer>();
            busRegistrationConfigurator.AddRequestClient<CompanyRegistrationRequested>();
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

        return services.BuildServiceProvider(validateScopes: true);
    }
}
