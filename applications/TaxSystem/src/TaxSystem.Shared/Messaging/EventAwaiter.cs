using System.Collections.Concurrent;

namespace TaxSystem.Shared.Messaging;

/// <summary>
/// Generic event awaiter that lets a caller publish a command and then
/// asynchronously wait for a correlated response event before continuing.
/// Register as <c>Singleton</c> per <typeparamref name="TEvent"/> type.
/// </summary>
public sealed class EventAwaiter<TEvent> where TEvent : class, ICorrelatedEvent
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<TEvent>> _pending = new();

    /// <summary>
    /// Registers interest in an event with the given <paramref name="correlationKey"/>
    /// and returns a task that completes when <see cref="Complete"/> is called with a
    /// matching key.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a request with the same correlation key is already being tracked.
    /// </exception>
    public Task<TEvent> Track(string correlationKey)
    {
        var tcs = new TaskCompletionSource<TEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pending.TryAdd(correlationKey, tcs))
        {
            throw new InvalidOperationException(
                $"A request with correlation key '{correlationKey}' is already in progress.");
        }

        return tcs.Task;
    }

    /// <summary>
    /// Completes the pending wait for the correlation key extracted from the event.
    /// Returns <c>true</c> if a pending entry was found and completed.
    /// </summary>
    public bool Complete(TEvent evt)
    {
        if (_pending.TryRemove(evt.CorrelationKey, out var tcs))
        {
            tcs.TrySetResult(evt);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cancels and removes a pending wait (e.g., on timeout).
    /// </summary>
    public void Cancel(string correlationKey)
    {
        if (_pending.TryRemove(correlationKey, out var tcs))
        {
            tcs.TrySetCanceled();
        }
    }

    /// <summary>
    /// Tracks a correlation key, publishes a command, and waits for the
    /// correlated event — or throws <see cref="TimeoutException"/> if it
    /// does not arrive within <paramref name="timeout"/>.
    /// </summary>
    public async Task<TEvent> PublishAndWait(
        Func<Task> publishAction,
        string correlationKey,
        TimeSpan timeout)
    {
        var tracked = Track(correlationKey);

        await publishAction();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await tracked.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Cancel(correlationKey);
            throw new TimeoutException(
                $"No {typeof(TEvent).Name} received for key '{correlationKey}' within {timeout.TotalSeconds}s.");
        }
    }
}

