using MassTransit;
using TaxSystem.Shared.Messaging.Contracts;

namespace TaxSystem.Tests.Messaging;

[TestFixture]
public sealed class MessageQueueTests
{
    [Test]
    public async Task InMemoryBusPublishesMessageToMatchingHandler()
    {
        var received = new TaskCompletionSource<CitizenRegistered>();
        var bus = Bus.Factory.CreateUsingInMemory(configurator =>
        {
            configurator.ReceiveEndpoint("citizen-registered-test", endpoint =>
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

            var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.That(message, Is.EqualTo(new CitizenRegistered("010101-1234", "John Doe")));
        }
        finally
        {
            await bus.StopAsync();
        }
    }
}
