using MassTransit;

namespace TaxSystem.Shared.Messaging;

/// <summary>
/// Generic MassTransit consumer that completes the corresponding
/// <see cref="EventAwaiter{TEvent}"/> when an event arrives.
/// One instance is registered per event type that needs publish-and-wait.
/// </summary>
public sealed class EventAwaiterConsumer<TEvent> : IConsumer<TEvent>
    where TEvent : class, ICorrelatedEvent
{
    private readonly EventAwaiter<TEvent> _awaiter;

    public EventAwaiterConsumer(EventAwaiter<TEvent> awaiter)
    {
        _awaiter = awaiter;
    }

    public Task Consume(ConsumeContext<TEvent> context)
    {
        _awaiter.Complete(context.Message);
        return Task.CompletedTask;
    }
}

