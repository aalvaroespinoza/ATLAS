namespace ATLAS.Core.Integrations.Telegram;

/// <summary>
/// Service contract for running a background long-polling listener against Telegram Bot API.
/// </summary>
public interface ITelegramListenerService : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the polling background worker is actively running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Starts the background long-polling loop.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully stops the background long-polling loop.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired whenever a new message is received from Telegram.
    /// </summary>
    event Action<TelegramMessage>? MessageReceived;
}
