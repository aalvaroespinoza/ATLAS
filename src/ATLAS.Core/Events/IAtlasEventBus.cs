namespace ATLAS.Core.Events;

/// <summary>
/// Thread-safe in-memory event bus for decoupled publishing and subscription of internal domain events.
/// </summary>
public interface IAtlasEventBus
{
    /// <summary>
    /// Publishes a domain event to all matching subscribers asynchronously.
    /// Handlers are executed concurrently with error isolation.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IAtlasEvent;

    /// <summary>
    /// Subscribes an asynchronous handler to events of type TEvent.
    /// Returns an IDisposable subscription token to cleanly unsubscribe.
    /// </summary>
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : IAtlasEvent;

    /// <summary>
    /// Subscribes an asynchronous handler to all domain events regardless of their concrete type.
    /// </summary>
    IDisposable SubscribeAll(Func<IAtlasEvent, Task> handler);
}
