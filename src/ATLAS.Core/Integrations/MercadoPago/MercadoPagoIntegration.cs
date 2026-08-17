using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.MercadoPago;

/// <summary>
/// Integration adapter for Mercado Pago financial synchronizer.
/// </summary>
public class MercadoPagoIntegration : IAtlasIntegration
{
    private readonly IMercadoPagoClient _client;
    private readonly ISecretVault _secretVault;

    public const string IntegrationId = "mercadopago";

    public MercadoPagoIntegration(IMercadoPagoClient client, ISecretVault secretVault)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
    }

    public string Id => IntegrationId;

    public string DisplayName => "Mercado Pago";

    public string Description => "Sincronización a demanda de cobros, transferencias y pagos en ARS.";

    public IntegrationCapabilities Capabilities { get; } = new(
        CanIngest: true,
        CanSend: false,
        CanSync: false,
        RequiresPolling: false,
        SupportsOAuth: false
    );

    public IReadOnlyList<string> RequiredSecrets { get; } = [SecretKeys.MercadoPagoAccessToken];

    public bool IsConfigured => _secretVault.HasSecret(SecretKeys.MercadoPagoAccessToken);

    public async Task<IntegrationHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.NotConfigured,
                Message: "Access Token de Mercado Pago no configurado.",
                Latency: null,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }

        var (success, message, latency) = await _client.PingAsync(cancellationToken).ConfigureAwait(false);

        return new IntegrationHealthReport(
            IntegrationId: Id,
            Status: success ? IntegrationHealthStatus.Healthy : IntegrationHealthStatus.Error,
            Message: message,
            Latency: latency,
            CheckedAt: DateTimeOffset.UtcNow
        );
    }
}
