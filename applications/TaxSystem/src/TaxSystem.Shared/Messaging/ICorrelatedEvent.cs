namespace TaxSystem.Shared.Messaging;

/// <summary>
/// Marker interface for event contracts that can be correlated
/// back to an originating request using a string key.
/// </summary>
public interface ICorrelatedEvent
{
    /// <summary>
    /// The key used to correlate this event back to the original request
    /// (e.g., CPR, CVR, or any domain identifier).
    /// </summary>
    string CorrelationKey { get; }
}

