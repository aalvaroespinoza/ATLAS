using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Storage.Database;
using Microsoft.Data.Sqlite;

namespace ATLAS.Storage.Repositories;

/// <summary>
/// SQLite implementation of IGoalRepository.
/// </summary>
public class GoalsRepository : IGoalRepository
{
    private readonly string _connectionString;

    public GoalsRepository(string? connectionString = null)
    {
        _connectionString = connectionString ?? DatabaseConfig.GetDefaultConnectionString();
    }

    public async Task<Goal> CreateAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);

        if (string.IsNullOrWhiteSpace(goal.Id))
        {
            throw new ArgumentException("Goal ID cannot be null or whitespace.", nameof(goal));
        }

        if (string.IsNullOrWhiteSpace(goal.Title))
        {
            throw new ArgumentException("Goal Title cannot be null or whitespace.", nameof(goal));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT INTO goals (id, title, description, status, progress, target_date, created_at)
            VALUES (@id, @title, @description, @status, @progress, @target_date, @created_at);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", goal.Id);
        command.Parameters.AddWithValue("@title", goal.Title.Trim());
        command.Parameters.AddWithValue("@description", (object?)goal.Description?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(goal.Status) ? "active" : goal.Status.Trim());
        command.Parameters.AddWithValue("@progress", Math.Clamp(goal.Progress, 0, 100));
        command.Parameters.AddWithValue("@target_date", goal.TargetDate.HasValue ? goal.TargetDate.Value.ToString("O", CultureInfo.InvariantCulture) : DBNull.Value);
        command.Parameters.AddWithValue("@created_at", goal.CreatedAt.ToString("O", CultureInfo.InvariantCulture));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return goal;
    }

    public async Task<Goal?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, title, description, status, progress, target_date, created_at
            FROM goals
            WHERE id = @id
            LIMIT 1;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return MapGoal(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<Goal>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var trimmedStatus = status?.Trim();
        string sql;

        if (string.IsNullOrEmpty(trimmedStatus))
        {
            sql = """
                SELECT id, title, description, status, progress, target_date, created_at
                FROM goals
                ORDER BY datetime(created_at) DESC;
                """;
        }
        else
        {
            sql = """
                SELECT id, title, description, status, progress, target_date, created_at
                FROM goals
                WHERE status = @status
                ORDER BY datetime(created_at) DESC;
                """;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (!string.IsNullOrEmpty(trimmedStatus))
        {
            command.Parameters.AddWithValue("@status", trimmedStatus);
        }

        var goals = new List<Goal>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            goals.Add(MapGoal(reader));
        }

        return goals.AsReadOnly();
    }

    public async Task<Goal> UpdateAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);

        if (string.IsNullOrWhiteSpace(goal.Id))
        {
            throw new ArgumentException("Goal ID cannot be null or whitespace.", nameof(goal));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            UPDATE goals
            SET title = @title,
                description = @description,
                status = @status,
                progress = @progress,
                target_date = @target_date
            WHERE id = @id;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", goal.Id);
        command.Parameters.AddWithValue("@title", goal.Title.Trim());
        command.Parameters.AddWithValue("@description", (object?)goal.Description?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(goal.Status) ? "active" : goal.Status.Trim());
        command.Parameters.AddWithValue("@progress", Math.Clamp(goal.Progress, 0, 100));
        command.Parameters.AddWithValue("@target_date", goal.TargetDate.HasValue ? goal.TargetDate.Value.ToString("O", CultureInfo.InvariantCulture) : DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        if (rows == 0)
        {
            throw new KeyNotFoundException($"Goal with ID '{goal.Id}' was not found.");
        }

        return goal;
    }

    private static Goal MapGoal(SqliteDataReader reader)
    {
        var id = reader.GetString(0);
        var title = reader.GetString(1);
        var description = reader.IsDBNull(2) ? null : reader.GetString(2);
        var status = reader.GetString(3);
        var progress = reader.GetInt32(4);
        var targetDateStr = reader.IsDBNull(5) ? null : reader.GetString(5);
        var createdAtStr = reader.GetString(6);

        DateTimeOffset? targetDate = null;
        if (!string.IsNullOrWhiteSpace(targetDateStr) &&
            DateTimeOffset.TryParse(targetDateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedTarget))
        {
            targetDate = parsedTarget;
        }

        var createdAt = DateTimeOffset.TryParse(createdAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCreated)
            ? parsedCreated
            : DateTimeOffset.UtcNow;

        return new Goal
        {
            Id = id,
            Title = title,
            Description = description,
            Status = status,
            Progress = progress,
            TargetDate = targetDate,
            CreatedAt = createdAt
        };
    }
}
