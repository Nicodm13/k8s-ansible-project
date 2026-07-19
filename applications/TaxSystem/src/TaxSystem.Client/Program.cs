using TaxSystem.Client.Services;
using TaxSystem.Shared.Messaging;
using TaxSystem.Shared.Messaging.Contracts;

namespace TaxSystem.Client;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddScoped<CompanyClientService>();
        builder.Services.AddSingleton<TaxInfoService>();
        builder.Services.AddScoped<CitizenClientService>();
        builder.Services.AddTaxSystemRabbitMq(builder.Configuration, registrationConfigurator =>
        {
            registrationConfigurator.AddRequestClient<CompanyInfoRequested>();
            registrationConfigurator.AddRequestClient<CompanyRegistrationRequested>();
            registrationConfigurator.AddRequestClient<CitizenRegistrationRequested>();
            registrationConfigurator.AddRequestClient<CitizenInfoRequested>();
        });
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

        app.MapGet("/healthz", () => Results.Ok());

        app.UseAuthorization();

        app.MapHealthChecks("/healthz");
        app.MapControllers();

        app.Run();
    }
}
