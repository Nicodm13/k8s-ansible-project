using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.StatementGenerator.Repositories;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.StatementGenerator;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTaxSystemRabbitMq(builder.Configuration);
        builder.Services.AddSingleton(_ => new FileSystemRepository("statements"));
        builder.Services.AddSingleton<StatementRepository>();
        builder.Services.AddSingleton<IReadStatementRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<StatementRepository>());
        builder.Services.AddSingleton<IWriteStatementRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<StatementRepository>());

        var host = builder.Build();

        await host.RunAsync();
    }
}
