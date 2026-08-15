using ATLAS.Core.Entities;

namespace ATLAS.Core.Repositories;

/// <summary>
/// Repository contract for persisting and retrieving notes.
/// </summary>
public interface INoteRepository
{
    /// <summary>
    /// Persists a new note into storage.
    /// </summary>
    Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent notes up to the specified limit.
    /// </summary>
    Task<IReadOnlyList<Note>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
}
