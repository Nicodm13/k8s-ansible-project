using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.BankService.Persistance;
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
        builder.Services.AddTaxSystemPostgres<BankDbContext>();
        builder.Services.AddScoped<IBankReadRepository, BankPostgresRepository>();
        builder.Services.AddScoped<IBankWriteRepository, BankPostgresRepository>();
        builder.Services.AddScoped<Services.BankService>();

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        await host.RunAsync();
    }
}
