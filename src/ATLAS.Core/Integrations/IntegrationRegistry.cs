using System.Collections.Concurrent;

namespace ATLAS.Core.Integrations;

/// <summary>
/// Default implementation of IIntegrationRegistry managing registered IAtlasIntegration adapters.
/// </summary>
public class IntegrationRegistry : IIntegrationRegistry
{
    private readonly Dictionary<string, IAtlasIntegration> _integrations = new(StringComparer.OrdinalIgnoreCase);

    public IntegrationRegistry(IEnumerable<IAtlasIntegration> integrations)
    {
        ArgumentNullException.ThrowIfNull(integrations);

        foreach (var integration in integrations)
        {
            _integrations[integration.Id] = integration;
        }
    }

    public IReadOnlyList<IAtlasIntegration> GetAllIntegrations()
        => _integrations.Values.ToList();

    public IAtlasIntegration? GetIntegration(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _integrations.TryGetValue(id, out var integration) ? integration : null;
    }

    public TIntegration? GetIntegration<TIntegration>(string id) where TIntegration : class, IAtlasIntegration
    {
        var integration = GetIntegration(id);
        return integration as TIntegration;
    }

    public async Task<IReadOnlyDictionary<string, IntegrationHealthReport>> CheckAllHealthAsync(CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentDictionary<string, IntegrationHealthReport>(StringComparer.OrdinalIgnoreCase);
        var tasks = new List<Task>();

        foreach (var integration in _integrations.Values)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var report = await integration.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
                    results[integration.Id] = report;
                }
                catch (Exception ex)
                {
                    results[integration.Id] = new IntegrationHealthReport(
                        IntegrationId: integration.Id,
                        Status: IntegrationHealthStatus.Error,
                        Message: $"Error no controlado durante health check: {ex.Message}",
                        Latency: null,
                        CheckedAt: DateTimeOffset.UtcNow
                    );
                }
            }, cancellationToken));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        return results;
    }
}
