namespace ATLAS.Core.Context;

/// <summary>
/// Service contract for aggregating transversal Home context data from SQLite repositories.
/// </summary>
public interface IHomeContextService
{
    /// <summary>
    /// Loads and calculates all real metrics, agenda items, habits, roadmaps, and activity feed for Home.
    /// </summary>
    Task<HomeContextData> LoadHomeContextAsync(CancellationToken cancellationToken = default);
}
