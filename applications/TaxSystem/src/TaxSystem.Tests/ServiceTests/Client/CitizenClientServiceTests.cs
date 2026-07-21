using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.Client.Services;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Models;

namespace TaxSystem.Tests.ServiceTests.Client;

public class CitizenClientServiceTests
{
    [Test]
    public async Task ReportDeductiblesReturnsTrueWhenStatementGeneratorRespondsForEveryDeductible()
    {
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("report-deductibles-client-test", endpoint =>
            {
                endpoint.Handler<ReportDeductibles>(context =>
                    context.RespondAsync(new DeductiblesReported(
                        context.Message.Cpr,
                        context.Message.Amount,
                        context.Message.DeductionType)));
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<CitizenClientService>();
            var result = await service.ReportDeductibles("101011234", 2026,
            [
                new Deductible { Amount = 10000m, DeductionType = "CharitableDonations" },
                new Deductible { Amount = 5000m, DeductionType = "Commuting" }
            ]);

            Assert.That(result, Is.True);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task ReportDeductiblesReturnsFalseWhenNoResponseIsReceived()
    {
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("report-deductibles-client-test", endpoint =>
            {
                endpoint.Handler<ReportDeductibles>(_ => Task.CompletedTask);
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<CitizenClientService>();

            Assert.ThrowsAsync<RequestTimeoutException>(async () =>
                await service.ReportDeductibles("101011234", 2026,
                [
                    new Deductible { Amount = 10000m, DeductionType = "CharitableDonations" }
                ]));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private static ServiceProvider BuildProvider(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var services = new ServiceCollection();
        services.AddScoped<CitizenClientService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddRequestClient<CitizenRegistrationRequested>();
            busRegistrationConfigurator.AddRequestClient<CitizenInfoRequested>();
            busRegistrationConfigurator.AddRequestClient<CitizenDeregistrationRequested>();
            busRegistrationConfigurator.AddRequestClient<ReportDeductibles>(RequestTimeout.After(s: 2));
            busRegistrationConfigurator.UsingInMemory((_, configurator) => configure(configurator));
        });

        return services.BuildServiceProvider(validateScopes: true);
    }
}

