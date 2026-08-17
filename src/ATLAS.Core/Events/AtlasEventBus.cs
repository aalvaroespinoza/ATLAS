using System.Collections.Concurrent;

namespace ATLAS.Core.Events;

/// <summary>
/// Default in-memory implementation of IAtlasEventBus.
/// Executes registered handlers asynchronously with error isolation and zero external dependencies.
/// </summary>
public sealed class AtlasEventBus : IAtlasEventBus
{
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Func<IAtlasEvent, Task>>> _subscriptions = new();

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IAtlasEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var tasks = new List<Task>();

        // 1. Specific type handlers
        if (_subscriptions.TryGetValue(typeof(TEvent), out var typeHandlers))
        {
            foreach (var handler in typeHandlers.Values)
            {
                tasks.Add(SafeInvokeAsync(handler, domainEvent));
            }
        }

        // 2. Global (IAtlasEvent) handlers if TEvent is not IAtlasEvent itself
        if (typeof(TEvent) != typeof(IAtlasEvent) && _subscriptions.TryGetValue(typeof(IAtlasEvent), out var globalHandlers))
        {
            foreach (var handler in globalHandlers.Values)
            {
                tasks.Add(SafeInvokeAsync(handler, domainEvent));
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : IAtlasEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        var type = typeof(TEvent);

        var handlers = _subscriptions.GetOrAdd(type, _ => new ConcurrentDictionary<Guid, Func<IAtlasEvent, Task>>());
        handlers[id] = (@event) => handler((TEvent)@event);

        return new SubscriptionToken(() =>
        {
            if (_subscriptions.TryGetValue(type, out var dict))
            {
                dict.TryRemove(id, out _);
            }
        });
    }

    public IDisposable SubscribeAll(Func<IAtlasEvent, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var id = Guid.NewGuid();
        var type = typeof(IAtlasEvent);

        var handlers = _subscriptions.GetOrAdd(type, _ => new ConcurrentDictionary<Guid, Func<IAtlasEvent, Task>>());
        handlers[id] = (@event) => handler(@event);

        return new SubscriptionToken(() =>
        {
            if (_subscriptions.TryGetValue(type, out var dict))
            {
                dict.TryRemove(id, out _);
            }
        });
    }

    private static async Task SafeInvokeAsync(Func<IAtlasEvent, Task> handler, IAtlasEvent @event)
    {
        try
        {
            await handler(@event).ConfigureAwait(false);
        }
        catch
        {
            // Subscriber exceptions are isolated to prevent blocking the event publisher
        }
    }

    private sealed class SubscriptionToken : IDisposable
    {
        private readonly Action _unsubscribe;
        private bool _disposed;

        public SubscriptionToken(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _unsubscribe();
            }
        }
    }
}
