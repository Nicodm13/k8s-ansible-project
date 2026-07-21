using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using TaxSystem.Client.Services;
using TaxSystem.Shared.Messaging.Contracts;

namespace TaxSystem.Tests.ServiceTests.Client;

public class CompanyClientServiceTests
{
    [Test]
    public async Task SetEmployeeIncomeForYearReturnsTrueWhenCompanyRespondsWithSalaryReported()
    {
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("report-salary-client-test", endpoint =>
            {
                endpoint.Handler<ReportSalary>(context =>
                    context.RespondAsync(new SalaryReported(context.Message.Cpr, "Acme Corp", (int)context.Message.Income)));
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<CompanyClientService>();
            var result = await service.SetEmployeeIncomeForYear("12345678", 2026, "101011234", 100000, null);

            Assert.That(result, Is.True);
        }
        finally
        {
            await bus.StopAsync();
        }
    }

    [Test]
    public async Task SetEmployeeIncomeForYearReturnsFalseWhenCompanyIsNotFound()
    {
        await using var provider = BuildProvider(configurator =>
        {
            configurator.ReceiveEndpoint("report-salary-client-test", endpoint =>
            {
                endpoint.Handler<ReportSalary>(context =>
                    context.RespondAsync(new CompanyInfoNotFound(context.Message.Cvr)));
            });
        });
        var bus = provider.GetRequiredService<IBusControl>();

        await bus.StartAsync();
        try
        {
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<CompanyClientService>();
            var result = await service.SetEmployeeIncomeForYear("99999999", 2026, "101011234", 100000, null);

            Assert.That(result, Is.False);
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
            busRegistrationConfigurator.AddRequestClient<ReportSalary>();
            busRegistrationConfigurator.UsingInMemory((_, configurator) => configure(configurator));
        });

        return services.BuildServiceProvider(validateScopes: true);
    }
}
