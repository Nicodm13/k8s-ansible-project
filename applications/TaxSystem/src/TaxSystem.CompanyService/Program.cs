using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxSystem.CompanyService.Persistance;
using TaxSystem.CompanyService.Repositories;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Persistance;
using BackendCompanyService = TaxSystem.CompanyService.Services.CompanyService;

namespace TaxSystem.CompanyService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTaxSystemRabbitMq(builder.Configuration);
        builder.Services.AddTaxSystemPostgres<CompanyDbContext>();
        builder.Services.AddScoped<IReadCompanyRepository, CompanyPostgresRepository>();
        builder.Services.AddScoped<IWriteCompanyRepository, CompanyPostgresRepository>();
        builder.Services.AddScoped<BackendCompanyService>();

        var host = builder.Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CompanyDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        await host.RunAsync();
    }
}
