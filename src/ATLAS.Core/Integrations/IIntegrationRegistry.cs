namespace ATLAS.Core.Integrations;

/// <summary>
/// Central registry and lifecycle manager for all configured external integrations.
/// </summary>
public interface IIntegrationRegistry
{
    /// <summary>
    /// Returns all registered integration adapters.
    /// </summary>
    IReadOnlyList<IAtlasIntegration> GetAllIntegrations();

    /// <summary>
    /// Retrieves a specific integration by its unique ID.
    /// </summary>
    IAtlasIntegration? GetIntegration(string id);

    /// <summary>
    /// Retrieves a specific integration cast to its typed adapter interface.
    /// </summary>
    TIntegration? GetIntegration<TIntegration>(string id) where TIntegration : class, IAtlasIntegration;

    /// <summary>
    /// Performs concurrent diagnostic health checks across all registered integrations.
    /// </summary>
    Task<IReadOnlyDictionary<string, IntegrationHealthReport>> CheckAllHealthAsync(CancellationToken cancellationToken = default);
}
