using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Storage.Database;
using Microsoft.Data.Sqlite;

namespace ATLAS.Storage.Repositories;

/// <summary>
/// SQLite implementation of INoteRepository supporting extended Knowledge attributes and search.
/// </summary>
public class NotesRepository : INoteRepository
{
    private readonly string _connectionString;

    public NotesRepository(string? connectionString = null)
    {
        _connectionString = connectionString ?? DatabaseConfig.GetDefaultConnectionString();
    }

    public async Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        if (string.IsNullOrWhiteSpace(note.Id))
        {
            throw new ArgumentException("Note ID cannot be null or whitespace.", nameof(note));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT INTO notes (id, title, content, type, tags, created_at, source)
            VALUES (@id, @title, @content, @type, @tags, @created_at, @source);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", note.Id);
        command.Parameters.AddWithValue("@title", (object?)note.Title ?? DBNull.Value);
        command.Parameters.AddWithValue("@content", note.Content ?? string.Empty);
        command.Parameters.AddWithValue("@type", string.IsNullOrWhiteSpace(note.Type) ? "note" : note.Type);
        command.Parameters.AddWithValue("@tags", (object?)note.Tags ?? DBNull.Value);
        command.Parameters.AddWithValue("@created_at", note.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@source", note.Source ?? "quick_capture");

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return note;
    }

    public async Task<IReadOnlyList<Note>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await SearchAsync(null, count, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Note>> SearchAsync(string? query, int count = 20, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return [];
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var trimmedQuery = query?.Trim();
        string sql;

        if (string.IsNullOrEmpty(trimmedQuery))
        {
            sql = """
                SELECT id, title, content, type, tags, created_at, source
                FROM notes
                ORDER BY datetime(created_at) DESC
                LIMIT @limit;
                """;
        }
        else
        {
            sql = """
                SELECT id, title, content, type, tags, created_at, source
                FROM notes
                WHERE title LIKE @pattern
                   OR content LIKE @pattern
                   OR tags LIKE @pattern
                ORDER BY datetime(created_at) DESC
                LIMIT @limit;
                """;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@limit", count);

        if (!string.IsNullOrEmpty(trimmedQuery))
        {
            command.Parameters.AddWithValue("@pattern", $"%{trimmedQuery}%");
        }

        var notes = new List<Note>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetString(0);
            var title = reader.IsDBNull(1) ? null : reader.GetString(1);
            var content = reader.GetString(2);
            var type = reader.IsDBNull(3) ? "note" : reader.GetString(3);
            var tags = reader.IsDBNull(4) ? null : reader.GetString(4);
            var createdAtStr = reader.GetString(5);
            var source = reader.IsDBNull(6) ? "quick_capture" : reader.GetString(6);

            var createdAt = DateTimeOffset.TryParse(createdAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
                ? dt
                : DateTimeOffset.UtcNow;

            notes.Add(new Note
            {
                Id = id,
                Title = title,
                Content = content,
                Type = type,
                Tags = tags,
                CreatedAt = createdAt,
                Source = source
            });
        }

        return notes.AsReadOnly();
    }
}
