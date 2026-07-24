using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.CitizenService.Consumers;
using TaxSystem.CitizenService.Persistance;
using TaxSystem.CitizenService.Repositories;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;
using TaxSystem.Shared.Persistance;
using BackendCitizenService = TaxSystem.CitizenService.Services.CitizenService;

namespace TaxSystem.Tests.ServiceTests.CitizenService;

public class CitizenServiceMessageFlowTests
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
    public async Task CitizenRegistrationRequestRespondsAfterCitizenIsPersisted()
    {
        await using var provider = BuildProvider(_ => { });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<CitizenRegistrationRequested>>();
            var response = await client.GetResponse<CitizenRegistered>(new CitizenRegistrationRequested(
                "101011234",
                "John",
                "Doe",
                "Main Street 1",
                "Copenhagen",
                "1000",
                "1234567890"));
            var citizen = await scope.ServiceProvider.GetRequiredService<IReadCitizenRepository>().GetByCprAsync("101011234");

            Assert.That(response.Message, Is.EqualTo(new CitizenRegistered("101011234", "John Doe")));
            Assert.That(citizen, Is.Not.Null);
            Assert.That(citizen!.cpr, Is.EqualTo("101011234"));
            Assert.That(citizen.firstName, Is.EqualTo("John"));
            Assert.That(citizen.lastName, Is.EqualTo("Doe"));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task CitizenInfoRequestReturnsPersistedCitizen()
    {
        await using var provider = BuildProvider(_ => { });
        var bus = provider.GetRequiredService<IBusControl>();
        using (var setupScope = provider.CreateScope())
        {
            await setupScope.ServiceProvider.GetRequiredService<IWriteCitizenRepository>().SaveAsync(new Citizen
            {
                cpr = "101011234",
                firstName = "John",
                lastName = "Doe",
                streetAddress = "Main Street 1",
                city = "Copenhagen",
                zipCode = "1000",
                bankAccountNumber = "1234567890"
            });
        }

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<CitizenInfoRequested>>();
            var response = await client.GetResponse<CitizenInfoReceived>(new CitizenInfoRequested("101011234"));

            Assert.That(response.Message, Is.EqualTo(new CitizenInfoReceived(
                "101011234",
                "John",
                "Doe",
                "Main Street 1",
                "Copenhagen",
                "1000",
                "1234567890")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task CitizenInfoRequestReturnsNotFoundWhenCitizenDoesNotExist()
    {
        await using var provider = BuildProvider(_ => { });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<CitizenInfoRequested>>();
            var response = await client.GetResponse<CitizenInfoNotFound>(new CitizenInfoRequested("999999999"));

            Assert.That(response.Message, Is.EqualTo(new CitizenInfoNotFound("999999999")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task CitizenDeregistrationRequestDeletesCitizenAndPublishesDeregisteredEvent()
    {
        var deregistered = new TaskCompletionSource<CitizenDeregistered>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("citizen-deregistered-test", endpoint =>
            {
                endpoint.Handler<CitizenDeregistered>(context =>
                {
                    deregistered.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();
        using (var setupScope = provider.CreateScope())
        {
            await setupScope.ServiceProvider.GetRequiredService<IWriteCitizenRepository>().SaveAsync(new Citizen
            {
                cpr = "101011234",
                firstName = "John",
                lastName = "Doe"
            });
        }

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<CitizenDeregistrationRequested>>();
            var response = await client.GetResponse<CitizenDeregistered>(new CitizenDeregistrationRequested("101011234"));
            var message = await deregistered.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var readScope = provider.CreateScope();
            var citizen = await readScope.ServiceProvider.GetRequiredService<IReadCitizenRepository>().GetByCprAsync("101011234");

            Assert.That(response.Message, Is.EqualTo(new CitizenDeregistered("101011234")));
            Assert.That(message, Is.EqualTo(new CitizenDeregistered("101011234")));
            Assert.That(citizen, Is.Null);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private ServiceProvider BuildProvider(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var services = new ServiceCollection();
        var dbContextOptions = new DbContextOptionsBuilder<CitizenDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;
        services.AddDbContext<CitizenDbContext>(options =>
        {
            options.UseSqlite(_sqliteConnection);
        });
        services.AddSingleton<IReadDbContextFactory<CitizenDbContext>>(new TestReadDbContextFactory<CitizenDbContext>(dbContextOptions));
        services.AddScoped<IReadCitizenRepository, CitizenPostgresRepository>();
        services.AddScoped<IWriteCitizenRepository, CitizenPostgresRepository>();
        services.AddScoped<BackendCitizenService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<CitizenRegistrationRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<CitizenInfoRequestedConsumer>();
            busRegistrationConfigurator.AddConsumer<CitizenDeregistrationRequestedConsumer>();
            busRegistrationConfigurator.AddRequestClient<CitizenRegistrationRequested>();
            busRegistrationConfigurator.AddRequestClient<CitizenInfoRequested>();
            busRegistrationConfigurator.AddRequestClient<CitizenDeregistrationRequested>();
            busRegistrationConfigurator.UsingInMemory((context, configurator) =>
            {
                configurator.ReceiveEndpoint("citizen-service", endpoint =>
                {
                    endpoint.ConfigureConsumer<CitizenRegistrationRequestedConsumer>(context);
                    endpoint.ConfigureConsumer<CitizenInfoRequestedConsumer>(context);
                    endpoint.ConfigureConsumer<CitizenDeregistrationRequestedConsumer>(context);
                });

                configure(configurator);
            });
        });

        var provider = services.BuildServiceProvider(validateScopes: true);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CitizenDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }
}
