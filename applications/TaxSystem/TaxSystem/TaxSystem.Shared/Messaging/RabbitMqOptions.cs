using Microsoft.Extensions.Configuration;

namespace TaxSystem.Shared.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "eventsExchange";

    public static RabbitMqOptions FromEnvironment()
    {
        return new RabbitMqOptions
        {
            HostName = GetEnvironmentValue("RABBITMQ_HOST", "localhost"),
            Port = GetEnvironmentInt("RABBITMQ_PORT", 5672),
            UserName = GetEnvironmentValue("RABBITMQ_USERNAME", string.Empty),
            Password = GetEnvironmentValue("RABBITMQ_PASSWORD", string.Empty),
            VirtualHost = GetEnvironmentValue("RABBITMQ_VIRTUAL_HOST", "/"),
            ExchangeName = GetEnvironmentValue("RABBITMQ_EXCHANGE", "eventsExchange")
        };
    }

    public static RabbitMqOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("RabbitMq");

        return new RabbitMqOptions
        {
            HostName = GetEnvironmentValue("RABBITMQ_HOST", section["Host"] ?? "localhost"),
            Port = GetEnvironmentInt("RABBITMQ_PORT", GetConfigurationInt(section, "Port", 5672)),
            UserName = GetRequiredCredential("RABBITMQ_USERNAME"),
            Password = GetRequiredCredential("RABBITMQ_PASSWORD"),
            VirtualHost = GetEnvironmentValue("RABBITMQ_VIRTUAL_HOST", section["VirtualHost"] ?? "/"),
            ExchangeName = GetEnvironmentValue("RABBITMQ_EXCHANGE", section["ExchangeName"] ?? "eventsExchange")
        };
    }

    private static string GetEnvironmentValue(string name, string fallback)
    {
        return Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : fallback;
    }

    private static int GetEnvironmentInt(string name, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(name), out var value)
            ? value
            : fallback;
    }

    private static int GetConfigurationInt(IConfiguration configuration, string key, int fallback)
    {
        return int.TryParse(configuration[key], out var value)
            ? value
            : fallback;
    }

    private static string GetRequiredCredential(string environmentName)
    {
        var value = Environment.GetEnvironmentVariable(environmentName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"RabbitMQ credential is missing. Set it with the {environmentName} environment variable.");
        }

        return value;
    }
}
