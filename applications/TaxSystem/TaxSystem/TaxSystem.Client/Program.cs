using TaxSystem.Client.Services;
using TaxSystem.Shared.Messaging;

namespace TaxSystem.Client;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddTaxSystemRabbitMq(builder.Configuration);
        builder.Services.AddScoped<CompanyService>();
        builder.Services.AddScoped<TaxInfoService>();
        builder.Services.AddScoped<CitizenService>();
        builder.Services.AddControllers();
        builder.Services.AddHealthChecks();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();

        app.MapHealthChecks("/healthz");
        app.MapControllers();

        app.Run();
    }
}
