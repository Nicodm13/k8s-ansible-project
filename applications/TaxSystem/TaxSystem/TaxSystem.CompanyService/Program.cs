using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.CompanyService.Repositories;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.CompanyService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTaxSystemRabbitMq(builder.Configuration);
        builder.Services.AddSingleton(_ => new FileSystemRepository("companies"));
        builder.Services.AddSingleton<CompanyRepository>();
        builder.Services.AddSingleton<IReadCompanyRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<CompanyRepository>());
        builder.Services.AddSingleton<IWriteCompanyRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<CompanyRepository>());

        var host = builder.Build();

        await host.RunAsync();
    }
}
