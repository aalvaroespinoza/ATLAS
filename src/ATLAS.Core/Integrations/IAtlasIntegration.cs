namespace ATLAS.Core.Integrations;

/// <summary>
/// Universal contract that must be implemented by each external integration adapter in ATLAS.
/// Keeps Core completely agnostic of HTTP protocols, provider payloads, and vendor secrets.
/// </summary>
public interface IAtlasIntegration
{
    /// <summary>
    /// Unique identifier for this integration (e.g. "telegram", "gmail", "mercadopago", "supabase").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// User-facing display name.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Concise description of the integration's role in ATLAS.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Declarative operational capabilities.
    /// </summary>
    IntegrationCapabilities Capabilities { get; }

    /// <summary>
    /// Keys required to be present in ISecretVault for this integration to function.
    /// </summary>
    IReadOnlyList<string> RequiredSecrets { get; }

    /// <summary>
    /// Indicates whether all necessary configuration and secrets are present.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Performs an active diagnostic connectivity/authentication check against the external service.
    /// </summary>
    Task<IntegrationHealthReport> CheckHealthAsync(CancellationToken cancellationToken = default);
}
