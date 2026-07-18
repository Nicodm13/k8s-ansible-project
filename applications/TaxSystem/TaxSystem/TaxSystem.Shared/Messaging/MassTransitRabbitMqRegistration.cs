using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaxSystem.Shared.Messaging;

public static class MassTransitRabbitMqRegistration
{
    public static IServiceCollection AddTaxSystemRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = RabbitMqOptions.FromConfiguration(configuration);

        services.AddMassTransit(busRegistrationConfigurator =>
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is not null)
            {
                busRegistrationConfigurator.AddConsumers(entryAssembly);
            }

            busRegistrationConfigurator.UsingRabbitMq((context, rabbitMqConfigurator) =>
            {
                rabbitMqConfigurator.Host(options.HostName, (ushort)options.Port, options.VirtualHost, hostConfigurator =>
                {
                    hostConfigurator.Username(options.UserName);
                    hostConfigurator.Password(options.Password);
                });

                rabbitMqConfigurator.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
