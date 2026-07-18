using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaxSystem.Shared.Messaging;

public static class MassTransitRabbitMqRegistration
{
    public static IServiceCollection AddTaxSystemRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configure = null)
    {
        var options = RabbitMqOptions.FromConfiguration(configuration);

        services.AddMassTransit(busRegistrationConfigurator =>
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly is not null)
            {
                busRegistrationConfigurator.AddConsumers(entryAssembly);
            }

            configure?.Invoke(busRegistrationConfigurator);

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

    /// <summary>
    /// Registers an <see cref="EventAwaiter{TEvent}"/> singleton and its
    /// <see cref="EventAwaiterConsumer{TEvent}"/> so that any service can
    /// publish a command and await a correlated response event.
    /// Call inside the <c>configure</c> callback of <see cref="AddTaxSystemRabbitMq"/>.
    /// </summary>
    public static void AddEventAwaiter<TEvent>(
        this IBusRegistrationConfigurator configurator,
        IServiceCollection services)
        where TEvent : class, ICorrelatedEvent
    {
        services.AddSingleton<EventAwaiter<TEvent>>();
        configurator.AddConsumer<EventAwaiterConsumer<TEvent>>();
    }
}
