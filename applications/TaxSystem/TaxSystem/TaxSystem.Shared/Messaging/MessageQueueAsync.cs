using System.Collections.Concurrent;

namespace TaxSystem.Shared.Messaging;

public sealed class MessageQueueAsync : IMessageQueue, IDisposable
{
    private readonly Dictionary<string, List<Action<Event>>> handlersByTopic = [];
    private readonly BlockingCollection<Event> queue = [];
    private readonly Thread notificationThread;

    public MessageQueueAsync()
    {
        notificationThread = new Thread(ConsumeEvents)
        {
            IsBackground = true
        };
        notificationThread.Start();
    }

    public void Publish(Event @event)
    {
        queue.Add(@event);
    }

    public void AddHandler(string topic, Action<Event> handler)
    {
        if (!handlersByTopic.ContainsKey(topic))
        {
            handlersByTopic[topic] = [];
        }

        handlersByTopic[topic].Add(handler);
    }

    public void Dispose()
    {
        queue.CompleteAdding();
        notificationThread.Join(TimeSpan.FromSeconds(1));
        queue.Dispose();
    }

    private void ConsumeEvents()
    {
        foreach (var @event in queue.GetConsumingEnumerable())
        {
            foreach (var handler in handlersByTopic.GetValueOrDefault(@event.Topic, []))
            {
                handler(@event);
            }
        }
    }
}
