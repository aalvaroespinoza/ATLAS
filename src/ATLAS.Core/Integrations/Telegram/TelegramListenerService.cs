using System.Net.Http.Json;
using System.Text.Json;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Telegram;

/// <summary>
/// Background long-polling service for Telegram Bot API using native HttpClient.
/// </summary>
public class TelegramListenerService : ITelegramListenerService
{
    private readonly ISecretVault _secretVault;
    private readonly HttpClient _httpClient;
    private readonly int _pollingTimeoutSeconds;
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private long _lastUpdateId;

    public bool IsRunning { get; private set; }

    public event Action<TelegramMessage>? MessageReceived;

    public TelegramListenerService(
        ISecretVault secretVault,
        HttpClient? httpClient = null,
        int pollingTimeoutSeconds = 25)
    {
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        _httpClient = httpClient ?? new HttpClient();
        _pollingTimeoutSeconds = pollingTimeoutSeconds;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        IsRunning = true;
        _pollingTask = Task.Run(() => PollingLoopAsync(_cts.Token), CancellationToken.None);

        System.Diagnostics.Debug.WriteLine("[TelegramListenerService] Polling en background iniciado.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning || _cts == null)
        {
            return;
        }

        try
        {
            _cts.Cancel();
            if (_pollingTask != null)
            {
                await _pollingTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // Ignore timeout on cancel
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _pollingTask = null;
            IsRunning = false;
            System.Diagnostics.Debug.WriteLine("[TelegramListenerService] Polling detenido.");
        }
    }

    private async Task PollingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var token = _secretVault.GetSecret(SecretKeys.TelegramBotToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                // Wait before rechecking if user configured token
                try
                {
                    await Task.Delay(4000, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                continue;
            }

            try
            {
                var updates = await FetchUpdatesAsync(token.Trim(), _lastUpdateId + 1, cancellationToken).ConfigureAwait(false);

                foreach (var update in updates)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    if (update.UpdateId >= _lastUpdateId)
                    {
                        _lastUpdateId = update.UpdateId;
                    }

                    // Log the received message (safe logging without logging full token)
                    System.Diagnostics.Debug.WriteLine(
                        $"[Telegram] Mensaje #{update.MessageId} de @{update.FromUsername ?? "unknown"} en chat {update.ChatId}: \"{update.Text}\" ({update.Date:HH:mm:ss})");

                    try
                    {
                        MessageReceived?.Invoke(update);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Telegram] Error al despachar evento de mensaje: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Telegram] Error de red en polling: {ex.Message}");
                try
                {
                    await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Telegram] Excepción inesperada en polling: {ex.Message}");
                try
                {
                    await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    public async Task<IReadOnlyList<TelegramMessage>> FetchUpdatesAsync(
        string botToken,
        long offset,
        CancellationToken cancellationToken)
    {
        var endpoint = $"https://api.telegram.org/bot{botToken}/getUpdates";
        var payload = new
        {
            offset = offset > 0 ? (long?)offset : null,
            timeout = _pollingTimeoutSeconds,
            allowed_updates = new[] { "message" }
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Fallo al conectar con Telegram Bot API: {ex.Message}", ex);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Telegram API devolvió error {response.StatusCode}: {json}");
        }

        return ParseUpdatesJson(json);
    }

    public static IReadOnlyList<TelegramMessage> ParseUpdatesJson(string json)
    {
        var messages = new List<TelegramMessage>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ok", out var okProp) || !okProp.GetBoolean())
            {
                return messages;
            }

            if (!root.TryGetProperty("result", out var resultArr) || resultArr.ValueKind != JsonValueKind.Array)
            {
                return messages;
            }

            foreach (var update in resultArr.EnumerateArray())
            {
                if (!update.TryGetProperty("update_id", out var updateIdProp))
                {
                    continue;
                }

                var updateId = updateIdProp.GetInt64();

                if (!update.TryGetProperty("message", out var msgObj) || msgObj.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var messageId = msgObj.TryGetProperty("message_id", out var msgIdProp) ? msgIdProp.GetInt64() : 0;
                
                long chatId = 0;
                if (msgObj.TryGetProperty("chat", out var chatObj) && chatObj.TryGetProperty("id", out var chatIdProp))
                {
                    chatId = chatIdProp.GetInt64();
                }

                string? username = null;
                if (msgObj.TryGetProperty("from", out var fromObj) && fromObj.TryGetProperty("username", out var userProp))
                {
                    username = userProp.GetString();
                }

                var text = msgObj.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? string.Empty : string.Empty;

                var unixDate = msgObj.TryGetProperty("date", out var dateProp) ? dateProp.GetInt64() : 0;
                var date = unixDate > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(unixDate)
                    : DateTimeOffset.UtcNow;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    messages.Add(new TelegramMessage(updateId, messageId, chatId, username, text.Trim(), date));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Telegram] Error al parsear JSON de updates: {ex.Message}");
        }

        return messages;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
