using System.Diagnostics;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Supabase;

/// <summary>
/// Integration adapter for Supabase PostgreSQL & PostgREST cloud synchronization.
/// </summary>
public class SupabaseIntegration : IAtlasIntegration
{
    private readonly ISupabaseAuthService _authService;
    private readonly ISupabaseSyncService _syncService;
    private readonly ISecretVault _secretVault;
    private readonly HttpClient _httpClient;

    public const string IntegrationId = "supabase";

    public SupabaseIntegration(
        ISupabaseAuthService authService,
        ISupabaseSyncService syncService,
        ISecretVault secretVault,
        HttpClient? httpClient = null)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        _httpClient = httpClient ?? new HttpClient();
    }

    public string Id => IntegrationId;

    public string DisplayName => "Supabase Cloud";

    public string Description => "Sincronización bidireccional cifrada con PostgreSQL y autenticación GoTrue.";

    public IntegrationCapabilities Capabilities { get; } = new(
        CanIngest: true,
        CanSend: true,
        CanSync: true,
        RequiresPolling: false,
        SupportsOAuth: false
    );

    public IReadOnlyList<string> RequiredSecrets { get; } = [
        SecretKeys.SupabaseUrl,
        SecretKeys.SupabaseAnonKey
    ];

    public bool IsConfigured => _secretVault.HasSecret(SecretKeys.SupabaseUrl) &&
                               _secretVault.HasSecret(SecretKeys.SupabaseAnonKey);

    public async Task<IntegrationHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.NotConfigured,
                Message: "Supabase URL o Anon Key no configurados en la bóveda.",
                Latency: null,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }

        var url = _secretVault.GetSecret(SecretKeys.SupabaseUrl)?.Trim();
        var key = _secretVault.GetSecret(SecretKeys.SupabaseAnonKey)?.Trim();

        var sw = Stopwatch.StartNew();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{url?.TrimEnd('/')}/auth/v1/health");
            req.Headers.Add("apikey", key);

            var resp = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            if (resp.IsSuccessStatusCode)
            {
                var isAuthActive = _authService.IsAuthenticated();
                return new IntegrationHealthReport(
                    IntegrationId: Id,
                    Status: isAuthActive ? IntegrationHealthStatus.Healthy : IntegrationHealthStatus.Degraded,
                    Message: isAuthActive ? "Supabase activo y sesión iniciada." : "Supabase alcanzable (sin sesión iniciada).",
                    Latency: sw.Elapsed,
                    CheckedAt: DateTimeOffset.UtcNow
                );
            }

            return new IntegrationHealthReport(
                IntegrationId: Id,
                Status: IntegrationHealthStatus.Error,
                Message: $"Supabase retornó estado {(int)resp.StatusCode} ({resp.ReasonPhrase})",
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
                Message: $"Error de conexión con Supabase: {ex.Message}",
                Latency: sw.Elapsed,
                CheckedAt: DateTimeOffset.UtcNow
            );
        }
    }
}
