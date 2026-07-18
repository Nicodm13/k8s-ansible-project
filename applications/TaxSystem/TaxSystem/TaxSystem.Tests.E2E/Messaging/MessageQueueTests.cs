using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;
using TaxSystem.Shared.Messaging;

namespace TaxSystem.Tests.E2E.Messaging;

[TestFixture]
public sealed class MessageQueueTests
{
    [Test]
    public async Task MassTransitRabbitMqPublishesMessageToMatchingHandler()
    {
        var options = RabbitMqOptions.FromEnvironment();
        var queueName = $"taxsystem-test-{Guid.NewGuid():N}";
        var received = new TaskCompletionSource<CitizenRegistered>();
        var bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            configurator.Host(options.HostName, (ushort)options.Port, options.VirtualHost, hostConfigurator =>
            {
                hostConfigurator.Username(options.UserName);
                hostConfigurator.Password(options.Password);
            });

            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Handler<CitizenRegistered>(context =>
                {
                    received.SetResult(context.Message);
                    return Task.CompletedTask;
                });
            });
        });

        await bus.StartAsync();
        try
        {
            await bus.Publish(new CitizenRegistered("010101-1234", "John Doe"));

            var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.That(message, Is.EqualTo(new CitizenRegistered("010101-1234", "John Doe")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }
}

