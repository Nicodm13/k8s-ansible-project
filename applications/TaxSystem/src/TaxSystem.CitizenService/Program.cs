using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.CitizenService.Repositories;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.CitizenService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTaxSystemRabbitMq(builder.Configuration);
        builder.Services.AddSingleton(_ => new FileSystemRepository("citizens"));
        builder.Services.AddSingleton<CitizenRepository>();
        builder.Services.AddSingleton<IReadCitizenRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<CitizenRepository>());
        builder.Services.AddSingleton<IWriteCitizenRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<CitizenRepository>());
        builder.Services.AddSingleton<Services.CitizenService>();

        var host = builder.Build();

        await host.RunAsync();
    }
}
