using System.Diagnostics;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Telegram;

/// <summary>
/// Integration adapter for Telegram Bot capture and commands.
/// </summary>
public class TelegramIntegration : IAtlasIntegration
{
    private readonly ITelegramListenerService _listenerService;
    private readonly ISecretVault _secretVault;
    private readonly HttpClient _httpClient;

    public const string IntegrationId = "telegram";

    public TelegramIntegration(
        ITelegramListenerService listenerService,
        ISecretVault secretVault,
        HttpClient? httpClient = null)
    {
        _listenerService = listenerService ?? throw new ArgumentNullException(nameof(listenerService));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        _httpClient = httpClient ?? new HttpClient();
    }

    public string Id => IntegrationId;

    public string DisplayName => "Telegram Bot";

    public string Description => "Captura remota de notas, completado de hábitos y registro de gastos por chat.";

    public IntegrationCapabilities Capabilities { get; } = new(
        CanIngest: true,
        CanSend: true,
        CanSync: false,
        RequiresPolling: true,
        SupportsOAuth: false
    );

    public IReadOnlyList<string> RequiredSecrets { get; } = [SecretKeys.TelegramBotToken];

    public bool IsConfigured => _secretVault.HasSecret(SecretKeys.TelegramBotToken);

    public async Task<IntegrationHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.NotConfigured,
                Message: "Bot Token de Telegram no configurado en la bóveda.",
                Latency: null,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }

        var token = _secretVault.GetSecret(SecretKeys.TelegramBotToken)?.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.AuthenticationRequired,
                Message: "Bot Token vacío o inválido.",
                Latency: null,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync($"https://api.telegram.org/bot{token}/getMe", cancellationToken).ConfigureAwait(false);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                var isRunning = _listenerService.IsRunning;
                return new IntegrationHealthReport(
                    IntegrationId: Id,
                    Status: isRunning ? IntegrationHealthStatus.Healthy : IntegrationHealthStatus.Degraded,
                    Message: isRunning ? "Bot conectado y listener activo." : "Bot conectado (listener en pausa).",
                    Latency: sw.Elapsed,
                    CheckedAt: DateTimeOffset.UtcNow
                );
            }

            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.Error,
                Message: $"Telegram API devolvió código {(int)response.StatusCode} ({response.ReasonPhrase})",
                Latency: sw.Elapsed,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.Error,
                Message: $"Error al contactar Telegram: {ex.Message}",
                Latency: sw.Elapsed,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }
    }
}
