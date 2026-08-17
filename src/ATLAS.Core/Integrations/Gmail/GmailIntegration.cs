using System.Diagnostics;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Gmail;

/// <summary>
/// Integration adapter for Gmail OAuth 2.0 triage and email extraction.
/// </summary>
public class GmailIntegration : IAtlasIntegration
{
    private readonly IGmailClient _gmailClient;
    private readonly ISecretVault _secretVault;

    public const string IntegrationId = "gmail";

    public GmailIntegration(IGmailClient gmailClient, ISecretVault secretVault)
    {
        _gmailClient = gmailClient ?? throw new ArgumentNullException(nameof(gmailClient));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
    }

    public string Id => IntegrationId;

    public string DisplayName => "Gmail";

    public string Description => "Triage y captura de correos recientes en modo solo lectura.";

    public IntegrationCapabilities Capabilities { get; } = new(
        CanIngest: true,
        CanSend: false,
        CanSync: false,
        RequiresPolling: false,
        SupportsOAuth: true
    );

    public IReadOnlyList<string> RequiredSecrets { get; } = [
        SecretKeys.GmailClientId,
        SecretKeys.GmailClientSecret,
        SecretKeys.GmailRefreshToken
    ];

    public bool IsConfigured => _secretVault.HasSecret(SecretKeys.GmailClientId) &&
                               _secretVault.HasSecret(SecretKeys.GmailRefreshToken);

    public async Task<IntegrationHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.NotConfigured,
                Message: "Credenciales OAuth de Gmail no configuradas.",
                Latency: null,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var messages = await _gmailClient.ListRecentMessagesAsync(limit: 1, cancellationToken: cancellationToken).ConfigureAwait(false);
            sw.Stop();

            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.Healthy,
                Message: $"Conectado con éxito a Gmail ({messages.Count} correos consultados).",
                Latency: sw.Elapsed,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }
        catch (GmailAuthException ex)
        {
            sw.Stop();
            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.AuthenticationRequired,
                Message: $"Autenticación requerida en Gmail: {ex.Message}",
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
                Message: $"Error al contactar Gmail API: {ex.Message}",
                Latency: sw.Elapsed,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }
    }
}
