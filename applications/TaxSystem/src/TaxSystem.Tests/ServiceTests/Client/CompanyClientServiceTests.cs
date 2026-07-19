using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.Client.Services;
using TaxSystem.Shared.Messaging.Contracts;

namespace TaxSystem.Tests.ServiceTests.Client;

public class CompanyClientServiceTests
{
    [Test]
    public async Task SetEmployeeIncomeForYearPublishesReportSalary()
    {
        var reportSalary = new TaskCompletionSource<ReportSalary>();
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("report-salary-client-test", endpoint =>
            {
                endpoint.Handler<ReportSalary>(context =>
                {
                    reportSalary.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<CompanyClientService>();
            await service.SetEmployeeIncomeForYear("12345678", 2026, 101011234, 100000);

            var message = await reportSalary.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.That(message, Is.EqualTo(new ReportSalary("12345678", 2026, "101011234", 100000m)));
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    private static ServiceProvider BuildProvider(Action<IInMemoryBusFactoryConfigurator> configure)
    {
        var services = new ServiceCollection();
        services.AddScoped<CompanyClientService>();
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddRequestClient<CompanyRegistrationRequested>();
            busRegistrationConfigurator.AddRequestClient<CompanyDeregistrationRequested>();
            busRegistrationConfigurator.AddRequestClient<CompanyInfoRequested>();
            busRegistrationConfigurator.UsingInMemory((_, configurator) => configure(configurator));
        });

        return services.BuildServiceProvider(validateScopes: true);
    }
}
