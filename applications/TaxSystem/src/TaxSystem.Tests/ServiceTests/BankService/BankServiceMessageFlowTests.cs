using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.BankService.Consumers;
using TaxSystem.BankService.Persistance;
using TaxSystem.BankService.Repositories;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Persistance;
using BackendBankService = TaxSystem.BankService.Services.BankService;

namespace TaxSystem.Tests.ServiceTests.BankService;

public class BankServiceMessageFlowTests
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
    public async Task ScheduleBankTransferPersistsTransferAndPublishesScheduledEvent()
    {
        var bankTransferScheduled = new TaskCompletionSource<BankTransferScheduled>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("bank-transfer-scheduled-test", endpoint =>
            {
                endpoint.Handler<BankTransferScheduled>(context =>
                {
                    bankTransferScheduled.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            await bus.Publish(new ScheduleBankTransfer("101011234", 13000m, "1234567890", "1234"));

            var message = await bankTransferScheduled.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using var scope = provider.CreateScope();
            var transfer = await scope.ServiceProvider.GetRequiredService<IBankReadRepository>().GetByCprAsync("101011234");

            Assert.That(message, Is.EqualTo(new BankTransferScheduled("101011234", 13000m, "1234567890", "1234")));
            Assert.That(transfer, Is.Not.Null);
            Assert.That(transfer!.Cpr, Is.EqualTo("101011234"));
            Assert.That(transfer.Amount, Is.EqualTo(13000m));
            Assert.That(transfer.AccountNumber, Is.EqualTo("1234567890"));
            Assert.That(transfer.RegistrationNumber, Is.EqualTo("1234"));
            Assert.That(transfer.Status, Is.EqualTo("Scheduled"));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task BankTransferInfoRequestReturnsPersistedTransfer()
    {
        await using var provider = BuildProvider(_ => { });
        var bus = provider.GetRequiredService<IBusControl>();
        using (var setupScope = provider.CreateScope())
        {
            await setupScope.ServiceProvider.GetRequiredService<IBankWriteRepository>().SaveAsync(new TaxSystem.Shared.Models.BankTransfer(
                "101011234",
                13000m,
                "1234567890",
                "1234",
                "Scheduled"));
        }

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<BankTransferInfoRequested>>();
            var response = await client.GetResponse<BankTransferInfoReceived>(new BankTransferInfoRequested("101011234"));

            Assert.That(response.Message, Is.EqualTo(new BankTransferInfoReceived(
                "101011234",
                13000m,
                "1234567890",
                "1234",
                "Scheduled")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task BankTransferInfoRequestReturnsNotFoundWhenTransferDoesNotExist()
    {
        await using var provider = BuildProvider(_ => { });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IRequestClient<BankTransferInfoRequested>>();
            var response = await client.GetResponse<BankTransferInfoNotFound>(new BankTransferInfoRequested("999999999"));

            Assert.That(response.Message, Is.EqualTo(new BankTransferInfoNotFound("999999999")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private ServiceProvider BuildProvider(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var services = new ServiceCollection();
        var dbContextOptions = new DbContextOptionsBuilder<BankDbContext>()
            .UseSqlite(_sqliteConnection)
            .Options;
        services.AddDbContext<BankDbContext>(options =>
        {
            options.UseSqlite(_sqliteConnection);
        });
        services.AddSingleton<IReadDbContextFactory<BankDbContext>>(new TestReadDbContextFactory<BankDbContext>(dbContextOptions));
        services.AddScoped<IBankReadRepository, BankPostgresRepository>();
        services.AddScoped<IBankWriteRepository, BankPostgresRepository>();
        services.AddScoped<BackendBankService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<ScheduleBankTransferConsumer>();
            busRegistrationConfigurator.AddConsumer<BankTransferInfoRequestedConsumer>();
            busRegistrationConfigurator.AddRequestClient<BankTransferInfoRequested>();
            busRegistrationConfigurator.UsingInMemory((context, configurator) =>
            {
                configurator.ReceiveEndpoint("bank-service", endpoint =>
                {
                    endpoint.ConfigureConsumer<ScheduleBankTransferConsumer>(context);
                    endpoint.ConfigureConsumer<BankTransferInfoRequestedConsumer>(context);
                });

                configure(configurator);
            });
        });

        var provider = services.BuildServiceProvider(validateScopes: true);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
        db.Database.EnsureCreated();

        return provider;
    }
}
