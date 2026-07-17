namespace TaxSystem.Shared.Messaging;

public interface IMessageQueue
{
    void Publish(Event @event);

    void AddHandler(string topic, Action<Event> handler);
}
