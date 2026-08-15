using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Storage.Database;
using Microsoft.Data.Sqlite;

namespace ATLAS.Storage.Repositories;

/// <summary>
/// SQLite implementation of IHabitRepository.
/// </summary>
public class HabitsRepository : IHabitRepository
{
    private readonly string _connectionString;

    public HabitsRepository(string? connectionString = null)
    {
        _connectionString = connectionString ?? DatabaseConfig.GetDefaultConnectionString();
    }

    public async Task<Habit> CreateAsync(Habit habit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(habit);

        if (string.IsNullOrWhiteSpace(habit.Id))
        {
            throw new ArgumentException("Habit ID cannot be null or whitespace.", nameof(habit));
        }

        if (string.IsNullOrWhiteSpace(habit.Name))
        {
            throw new ArgumentException("Habit Name cannot be null or whitespace.", nameof(habit));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT INTO habits (id, name, description, frequency, created_at)
            VALUES (@id, @name, @description, @frequency, @created_at);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", habit.Id);
        command.Parameters.AddWithValue("@name", habit.Name.Trim());
        command.Parameters.AddWithValue("@description", (object?)habit.Description?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@frequency", string.IsNullOrWhiteSpace(habit.Frequency) ? "daily" : habit.Frequency.Trim());
        command.Parameters.AddWithValue("@created_at", habit.CreatedAt.ToString("O", CultureInfo.InvariantCulture));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return habit;
    }

    public async Task<Habit?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, name, description, frequency, created_at
            FROM habits
            WHERE id = @id
            LIMIT 1;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return MapHabit(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<Habit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, name, description, frequency, created_at
            FROM habits
            ORDER BY datetime(created_at) DESC;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var habits = new List<Habit>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            habits.Add(MapHabit(reader));
        }

        return habits.AsReadOnly();
    }

    public async Task<HabitEvent> RecordEventAsync(HabitEvent habitEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(habitEvent);

        if (string.IsNullOrWhiteSpace(habitEvent.Id))
        {
            throw new ArgumentException("HabitEvent ID cannot be null or whitespace.", nameof(habitEvent));
        }

        if (string.IsNullOrWhiteSpace(habitEvent.HabitId))
        {
            throw new ArgumentException("HabitId cannot be null or whitespace.", nameof(habitEvent));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT INTO habit_events (id, habit_id, completed_at, note)
            VALUES (@id, @habit_id, @completed_at, @note);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", habitEvent.Id);
        command.Parameters.AddWithValue("@habit_id", habitEvent.HabitId);
        command.Parameters.AddWithValue("@completed_at", habitEvent.CompletedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@note", (object?)habitEvent.Note?.Trim() ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return habitEvent;
    }

    public async Task<IReadOnlyList<HabitEvent>> GetEventsAsync(
        string? habitId = null,
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var whereClauses = new List<string>();
        var trimmedHabitId = habitId?.Trim();

        if (!string.IsNullOrEmpty(trimmedHabitId))
        {
            whereClauses.Add("habit_id = @habit_id");
        }

        if (since.HasValue)
        {
            whereClauses.Add("datetime(completed_at) >= datetime(@since)");
        }

        var sql = "SELECT id, habit_id, completed_at, note FROM habit_events ";
        if (whereClauses.Count > 0)
        {
            sql += "WHERE " + string.Join(" AND ", whereClauses) + " ";
        }
        sql += "ORDER BY datetime(completed_at) DESC;";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        if (!string.IsNullOrEmpty(trimmedHabitId))
        {
            command.Parameters.AddWithValue("@habit_id", trimmedHabitId);
        }

        if (since.HasValue)
        {
            command.Parameters.AddWithValue("@since", since.Value.ToString("O", CultureInfo.InvariantCulture));
        }

        var events = new List<HabitEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetString(0);
            var hId = reader.GetString(1);
            var completedAtStr = reader.GetString(2);
            var note = reader.IsDBNull(3) ? null : reader.GetString(3);

            var completedAt = DateTimeOffset.TryParse(completedAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCompleted)
                ? parsedCompleted
                : DateTimeOffset.UtcNow;

            events.Add(new HabitEvent
            {
                Id = id,
                HabitId = hId,
                CompletedAt = completedAt,
                Note = note
            });
        }

        return events.AsReadOnly();
    }

    private static Habit MapHabit(SqliteDataReader reader)
    {
        var id = reader.GetString(0);
        var name = reader.GetString(1);
        var description = reader.IsDBNull(2) ? null : reader.GetString(2);
        var frequency = reader.GetString(3);
        var createdAtStr = reader.GetString(4);

        var createdAt = DateTimeOffset.TryParse(createdAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCreated)
            ? parsedCreated
            : DateTimeOffset.UtcNow;

        return new Habit
        {
            Id = id,
            Name = name,
            Description = description,
            Frequency = frequency,
            CreatedAt = createdAt
        };
    }
}
