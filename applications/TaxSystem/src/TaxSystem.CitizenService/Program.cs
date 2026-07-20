using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.CitizenService.Persistance;
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
        builder.Services.AddTaxSystemPostgres<CitizenDbContext>();
        builder.Services.AddScoped<IReadCitizenRepository, CitizenPostgresRepository>();
        builder.Services.AddScoped<IWriteCitizenRepository, CitizenPostgresRepository>();
        builder.Services.AddScoped<Services.CitizenService>();

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CitizenDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        await host.RunAsync();
    }
}
