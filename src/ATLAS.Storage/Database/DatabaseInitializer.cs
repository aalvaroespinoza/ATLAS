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

        // Enable foreign keys
        await using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
            await pragmaCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // 1. Create tables and indices
        const string schemaSql = """
            -- Notes table
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

            -- Goals table
            CREATE TABLE IF NOT EXISTS goals (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                description TEXT,
                status TEXT NOT NULL DEFAULT 'active',
                progress INTEGER NOT NULL DEFAULT 0,
                target_date TEXT,
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_goals_status ON goals(status);
            CREATE INDEX IF NOT EXISTS idx_goals_created_at ON goals(created_at);

            -- Habits table
            CREATE TABLE IF NOT EXISTS habits (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT,
                frequency TEXT NOT NULL DEFAULT 'daily',
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_habits_created_at ON habits(created_at);

            -- Habit Events table
            CREATE TABLE IF NOT EXISTS habit_events (
                id TEXT PRIMARY KEY,
                habit_id TEXT NOT NULL,
                completed_at TEXT NOT NULL,
                note TEXT,
                FOREIGN KEY (habit_id) REFERENCES habits(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS idx_habit_events_habit_id ON habit_events(habit_id);
            CREATE INDEX IF NOT EXISTS idx_habit_events_completed_at ON habit_events(completed_at);

            -- Transactions table
            CREATE TABLE IF NOT EXISTS transactions (
                id TEXT PRIMARY KEY,
                fecha TEXT NOT NULL,
                monto REAL NOT NULL,
                tipo TEXT NOT NULL DEFAULT 'expense',
                origen TEXT NOT NULL DEFAULT 'manual',
                descripcion TEXT NOT NULL,
                moneda TEXT NOT NULL DEFAULT 'ARS',
                categoria TEXT,
                subcategoria TEXT,
                id_externo TEXT,
                estado TEXT NOT NULL DEFAULT 'approved',
                metadata TEXT,
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_transactions_fecha ON transactions(fecha DESC);
            CREATE INDEX IF NOT EXISTS idx_transactions_tipo ON transactions(tipo);
            CREATE INDEX IF NOT EXISTS idx_transactions_origen ON transactions(origen);
            CREATE UNIQUE INDEX IF NOT EXISTS idx_transactions_id_externo ON transactions(id_externo) WHERE id_externo IS NOT NULL;
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
