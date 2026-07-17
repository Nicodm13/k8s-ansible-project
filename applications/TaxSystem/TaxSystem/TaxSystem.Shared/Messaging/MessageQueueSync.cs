namespace TaxSystem.Shared.Messaging;

public sealed class MessageQueueSync : IMessageQueue
{
    private readonly Dictionary<string, List<Action<Event>>> handlersByTopic = [];

    public void Publish(Event @event)
    {
        foreach (var handler in handlersByTopic.GetValueOrDefault(@event.Topic, []))
        {
            handler(@event);
        }
    }

    public void AddHandler(string topic, Action<Event> handler)
    {
        if (!handlersByTopic.ContainsKey(topic))
        {
            handlersByTopic[topic] = [];
        }

        handlersByTopic[topic].Add(handler);
    }
}
