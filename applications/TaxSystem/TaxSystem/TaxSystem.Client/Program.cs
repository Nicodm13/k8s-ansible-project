using TaxSystem.Client.Services;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Messaging;

namespace TaxSystem.Client;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddScoped<CompanyClientService>();
        builder.Services.AddScoped<TaxInfoService>();
        builder.Services.AddScoped<CitizenService>();
        builder.Services.AddTaxSystemRabbitMq(builder.Configuration, registrationConfigurator =>
        {
            registrationConfigurator.AddRequestClient<CompanyInfoRequested>();
        });
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
