using Microsoft.Data.Sqlite;

namespace ATLAS.Storage.Database;

/// <summary>
/// Handles directory creation and initial DDL schema migrations for SQLite.
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

        const string schemaSql = """
            CREATE TABLE IF NOT EXISTS notes (
                id TEXT PRIMARY KEY,
                content TEXT NOT NULL,
                created_at TEXT NOT NULL,
                source TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_notes_created_at ON notes(created_at DESC);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = schemaSql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
