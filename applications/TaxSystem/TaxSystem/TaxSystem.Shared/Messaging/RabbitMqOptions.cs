namespace TaxSystem.Shared.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; init; } = "localhost";

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "eventsExchange";
}
