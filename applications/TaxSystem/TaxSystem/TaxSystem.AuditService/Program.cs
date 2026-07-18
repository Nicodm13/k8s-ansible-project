using Microsoft.Extensions.Hosting;
using TaxSystem.Shared.Messaging;

namespace TaxSystem.AuditService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddTaxSystemRabbitMq(builder.Configuration);

        var host = builder.Build();

        await host.RunAsync();
    }
}
