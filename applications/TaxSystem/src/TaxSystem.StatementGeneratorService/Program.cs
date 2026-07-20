using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.StatementGenerator.Persistance;
using TaxSystem.StatementGenerator.Repositories;
using TaxSystem.StatementGenerator.Services;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Persistance;

namespace TaxSystem.StatementGenerator;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTaxSystemRabbitMq(builder.Configuration, registrationConfigurator =>
        {
            registrationConfigurator.AddRequestClient<CitizenInfoRequested>();
        });
        builder.Services.AddTaxSystemPostgres<StatementDbContext>();
        builder.Services.AddScoped<IReadStatementRepository, StatementPostgresRepository>();
        builder.Services.AddScoped<IWriteStatementRepository, StatementPostgresRepository>();
        builder.Services.AddScoped<StatementGeneratorService>();

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StatementDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        await host.RunAsync();
    }
}
