using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TaxSystem.Shared.Messaging;

public sealed class RabbitMqQueue : IMessageQueue, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions options;
    private readonly IConnection connection;
    private readonly IModel publishChannel;
    private readonly List<IModel> consumerChannels = [];

    public RabbitMqQueue()
        : this(new RabbitMqOptions())
    {
    }

    public RabbitMqQueue(string hostName)
        : this(new RabbitMqOptions { HostName = hostName })
    {
    }

    public RabbitMqQueue(RabbitMqOptions options)
    {
        this.options = options;

        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost
        };

        connection = factory.CreateConnection();
        publishChannel = CreateChannel();
    }

    public void Publish(Event @event)
    {
        Console.WriteLine($"[x] publish({@event})");

        var message = JsonSerializer.Serialize(@event, JsonOptions);
        var body = Encoding.UTF8.GetBytes(message);

        publishChannel.BasicPublish(
            exchange: options.ExchangeName,
            routingKey: @event.Topic,
            basicProperties: null,
            body: body);
    }

    public void AddHandler(string topic, Action<Event> handler)
    {
        Console.WriteLine($"[x] addHandler({topic})");

        var channel = CreateChannel();
        var queueName = channel.QueueDeclare().QueueName;
        channel.QueueBind(queueName, options.ExchangeName, topic);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (_, delivery) =>
        {
            var message = Encoding.UTF8.GetString(delivery.Body.ToArray());
            var @event = JsonSerializer.Deserialize<Event>(message, JsonOptions)
                ?? throw new InvalidOperationException("Received an invalid event message.");

            Console.WriteLine($"[x] executingHandler({@event})");
            handler(@event);
        };

        channel.BasicConsume(queueName, autoAck: true, consumer);
        consumerChannels.Add(channel);
    }

    public void Dispose()
    {
        foreach (var channel in consumerChannels)
        {
            channel.Dispose();
        }

        publishChannel.Dispose();
        connection.Dispose();
    }

    private IModel CreateChannel()
    {
        var channel = connection.CreateModel();
        channel.ExchangeDeclare(options.ExchangeName, ExchangeType.Topic);
        return channel;
    }
}
