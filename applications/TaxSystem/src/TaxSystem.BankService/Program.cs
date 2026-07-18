using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.BankService.Repositories;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.BankService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTaxSystemRabbitMq(builder.Configuration);
        builder.Services.AddSingleton(_ => new FileSystemRepository("bank-transfers"));
        builder.Services.AddSingleton<BankRepository>();
        builder.Services.AddSingleton<IBankReadRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<BankRepository>());
        builder.Services.AddSingleton<IBankWriteRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<BankRepository>());

        var host = builder.Build();

        await host.RunAsync();
    }
}
