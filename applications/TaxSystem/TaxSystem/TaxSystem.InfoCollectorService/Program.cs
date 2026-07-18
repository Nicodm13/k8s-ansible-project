using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.InfoCollectorService.Repositories;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.InfoCollectorService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTaxSystemRabbitMq(builder.Configuration);
        builder.Services.AddSingleton(_ => new FileSystemRepository("tax-info"));
        builder.Services.AddSingleton<InfoCollectorRepository>();
        builder.Services.AddSingleton<IReadInfoCollectorRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<InfoCollectorRepository>());
        builder.Services.AddSingleton<IWriteInfoCollectorRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<InfoCollectorRepository>());

        var host = builder.Build();

        await host.RunAsync();
    }
}
