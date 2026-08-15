using Microsoft.Data.Sqlite;

namespace ATLAS.Storage.Database;

/// <summary>
/// Handles directory creation and idempotent schema migrations for SQLite.
/// </summary>
public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string? connectionString = null)
    {
        _connectionString = connectionString ?? DatabaseConfig.GetDefaultConnectionString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        if (!string.IsNullOrEmpty(builder.DataSource) &&
            !builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase) &&
            !builder.DataSource.StartsWith("mode=memory", StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // 1. Create table if not exists (including new columns)
        const string schemaSql = """
            CREATE TABLE IF NOT EXISTS notes (
                id TEXT PRIMARY KEY,
                title TEXT,
                content TEXT NOT NULL,
                type TEXT NOT NULL DEFAULT 'note',
                tags TEXT,
                created_at TEXT NOT NULL,
                source TEXT NOT NULL DEFAULT 'quick_capture'
            );
            CREATE INDEX IF NOT EXISTS idx_notes_created_at ON notes(created_at DESC);
            """;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = schemaSql;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // 2. Perform non-destructive migrations for existing databases
        await MigrateNotesTableAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    private static async Task MigrateNotesTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(notes);";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Column name is in column index 1 ("name")
                existingColumns.Add(reader.GetString(1));
            }
        }

        if (!existingColumns.Contains("title"))
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE notes ADD COLUMN title TEXT;";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!existingColumns.Contains("type"))
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE notes ADD COLUMN type TEXT NOT NULL DEFAULT 'note';";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!existingColumns.Contains("tags"))
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE notes ADD COLUMN tags TEXT;";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!existingColumns.Contains("source"))
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "ALTER TABLE notes ADD COLUMN source TEXT NOT NULL DEFAULT 'quick_capture';";
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
