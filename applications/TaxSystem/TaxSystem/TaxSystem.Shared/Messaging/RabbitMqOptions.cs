namespace TaxSystem.Shared.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "eventsExchange";

    public static RabbitMqOptions FromEnvironment()
    {
        return new RabbitMqOptions
        {
            HostName = GetEnvironmentValue("RABBITMQ_HOST", "localhost"),
            Port = GetEnvironmentInt("RABBITMQ_PORT", 5672),
            UserName = GetEnvironmentValue("RABBITMQ_USERNAME", "guest"),
            Password = GetEnvironmentValue("RABBITMQ_PASSWORD", "guest"),
            VirtualHost = GetEnvironmentValue("RABBITMQ_VIRTUAL_HOST", "/"),
            ExchangeName = GetEnvironmentValue("RABBITMQ_EXCHANGE", "eventsExchange")
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
}
