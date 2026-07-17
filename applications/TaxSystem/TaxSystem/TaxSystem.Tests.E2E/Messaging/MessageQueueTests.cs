using TaxSystem.Shared.Messaging;

namespace TaxSystem.Tests.E2E.Messaging;

[TestFixture]
public sealed class MessageQueueTests
{
    [Test]
    public void RabbitMqQueuePublishesEventToMatchingHandler()
    {
        var topic = $"TaxSystem.Test.{Guid.NewGuid():N}";
        using var queue = new RabbitMqQueue(RabbitMqOptions.FromEnvironment());
        using var receivedSignal = new ManualResetEventSlim();
        Event? received = null;

        queue.AddHandler(topic, @event =>
        {
            received = @event;
            receivedSignal.Set();
        });

        queue.Publish(new Event(topic, new Citizen("010101-1234", "John Doe")));

        Assert.That(receivedSignal.Wait(TimeSpan.FromSeconds(5)), Is.True);
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Topic, Is.EqualTo(topic));
        Assert.That(received.GetArgument<Citizen>(0), Is.EqualTo(new Citizen("010101-1234", "John Doe")));
    }

    private sealed record Citizen(string Cpr, string Name);
}

