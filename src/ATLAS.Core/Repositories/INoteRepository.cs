using ATLAS.Core.Entities;

namespace ATLAS.Core.Repositories;

/// <summary>
/// Repository contract for persisting, retrieving and searching notes.
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

    /// <summary>
    /// Searches notes matching the query pattern against title, content, or tags.
    /// </summary>
    Task<IReadOnlyList<Note>> SearchAsync(string? query, int count = 20, CancellationToken cancellationToken = default);
}
