using TaxSystem.Shared.Messaging;

namespace TaxSystem.Tests.Messaging;

[TestFixture]
public sealed class MessageQueueTests
{
    [Test]
    public void SyncQueuePublishesEventToMatchingHandler()
    {
        var queue = new MessageQueueSync();
        Event? received = null;

        queue.AddHandler("CitizenRegistered", @event => received = @event);

        queue.Publish(new Event("CitizenRegistered", new Citizen("010101-1234", "John Doe")));

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Topic, Is.EqualTo("CitizenRegistered"));

        var citizen = received.GetArgument<Citizen>(0);
        Assert.That(citizen, Is.EqualTo(new Citizen("010101-1234", "John Doe")));
    }

    private sealed record Citizen(string Cpr, string Name);
}
