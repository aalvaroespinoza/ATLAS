using ATLAS.Core.Entities;

namespace ATLAS.Core.Repositories;

/// <summary>
/// Repository contract for Goal persistence and retrieval.
/// </summary>
public interface IGoalRepository
{
    /// <summary>
    /// Persists a new goal into storage.
    /// </summary>
    Task<Goal> CreateAsync(Goal goal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a goal by its unique identifier.
    /// </summary>
    Task<Goal?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all goals, optionally filtered by status (e.g. "active", "completed").
    /// </summary>
    Task<IReadOnlyList<Goal>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates progress, status, title, description or target date of an existing goal.
    /// </summary>
    Task<Goal> UpdateAsync(Goal goal, CancellationToken cancellationToken = default);
}
