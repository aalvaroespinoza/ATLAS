using ATLAS.Core.Entities;

namespace ATLAS.Core.Repositories;

/// <summary>
/// Repository contract for Habit definitions and raw completion events.
/// </summary>
public interface IHabitRepository
{
    /// <summary>
    /// Persists a new habit definition into storage.
    /// </summary>
    Task<Habit> CreateAsync(Habit habit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a habit definition by its unique identifier.
    /// </summary>
    Task<Habit?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all habit definitions.
    /// </summary>
    Task<IReadOnlyList<Habit>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a raw completion event for a habit.
    /// </summary>
    Task<HabitEvent> RecordEventAsync(HabitEvent habitEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves habit completion events, optionally filtered by habitId or date threshold.
    /// </summary>
    Task<IReadOnlyList<HabitEvent>> GetEventsAsync(string? habitId = null, DateTimeOffset? since = null, CancellationToken cancellationToken = default);
}
