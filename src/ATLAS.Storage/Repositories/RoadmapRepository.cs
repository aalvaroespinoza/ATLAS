using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Storage.Database;
using Microsoft.Data.Sqlite;

namespace ATLAS.Storage.Repositories;

/// <summary>
/// SQLite implementation of IRoadmapRepository.
/// </summary>
public class RoadmapRepository : IRoadmapRepository
{
    private readonly string _connectionString;

    public RoadmapRepository(string? connectionString = null)
    {
        _connectionString = connectionString ?? DatabaseConfig.GetDefaultConnectionString();
    }

    public async Task CreateAsync(Roadmap roadmap, IEnumerable<RoadmapMilestone>? milestones = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roadmap);

        if (string.IsNullOrWhiteSpace(roadmap.Id))
        {
            throw new ArgumentException("Roadmap ID cannot be null or whitespace.", nameof(roadmap));
        }

        if (string.IsNullOrWhiteSpace(roadmap.Title))
        {
            throw new ArgumentException("Roadmap Title cannot be null or whitespace.", nameof(roadmap));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        const string roadmapSql = """
            INSERT INTO roadmaps (id, goal_id, title, description, status, created_at, updated_at)
            VALUES (@id, @goal_id, @title, @description, @status, @created_at, @updated_at);
            """;

        await using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = roadmapSql;
            cmd.Parameters.AddWithValue("@id", roadmap.Id);
            cmd.Parameters.AddWithValue("@goal_id", (object?)roadmap.GoalId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@title", roadmap.Title.Trim());
            cmd.Parameters.AddWithValue("@description", (object?)roadmap.Description?.Trim() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(roadmap.Status) ? "active" : roadmap.Status.Trim());
            cmd.Parameters.AddWithValue("@created_at", roadmap.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("@updated_at", roadmap.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        if (milestones != null)
        {
            const string milestoneSql = """
                INSERT INTO roadmap_milestones (id, roadmap_id, title, order_index, status, notes, created_at, completed_at)
                VALUES (@id, @roadmap_id, @title, @order_index, @status, @notes, @created_at, @completed_at);
                """;

            foreach (var m in milestones)
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = milestoneSql;
                cmd.Parameters.AddWithValue("@id", m.Id);
                cmd.Parameters.AddWithValue("@roadmap_id", roadmap.Id);
                cmd.Parameters.AddWithValue("@title", m.Title.Trim());
                cmd.Parameters.AddWithValue("@order_index", m.OrderIndex);
                cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(m.Status) ? "pending" : m.Status.Trim());
                cmd.Parameters.AddWithValue("@notes", (object?)m.Notes?.Trim() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created_at", m.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@completed_at", m.CompletedAt.HasValue ? m.CompletedAt.Value.ToString("O", CultureInfo.InvariantCulture) : DBNull.Value);

                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Roadmap?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, goal_id, title, description, status, created_at, updated_at
            FROM roadmaps
            WHERE id = @id
            LIMIT 1;
            """;

        Roadmap? roadmap = null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Parameters.AddWithValue("@id", id);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                roadmap = MapRoadmap(reader);
            }
        }

        if (roadmap != null)
        {
            roadmap.Milestones = await FetchMilestonesForRoadmapAsync(connection, roadmap.Id, cancellationToken).ConfigureAwait(false);
        }

        return roadmap;
    }

    public async Task<Roadmap?> GetByGoalIdAsync(string goalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(goalId)) return null;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, goal_id, title, description, status, created_at, updated_at
            FROM roadmaps
            WHERE goal_id = @goal_id
            LIMIT 1;
            """;

        Roadmap? roadmap = null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            command.Parameters.AddWithValue("@goal_id", goalId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                roadmap = MapRoadmap(reader);
            }
        }

        if (roadmap != null)
        {
            roadmap.Milestones = await FetchMilestonesForRoadmapAsync(connection, roadmap.Id, cancellationToken).ConfigureAwait(false);
        }

        return roadmap;
    }

    public async Task<IReadOnlyList<Roadmap>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var trimmedStatus = status?.Trim();
        string sql = string.IsNullOrEmpty(trimmedStatus)
            ? "SELECT id, goal_id, title, description, status, created_at, updated_at FROM roadmaps ORDER BY datetime(created_at) DESC;"
            : "SELECT id, goal_id, title, description, status, created_at, updated_at FROM roadmaps WHERE status = @status ORDER BY datetime(created_at) DESC;";

        var list = new List<Roadmap>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            if (!string.IsNullOrEmpty(trimmedStatus))
            {
                command.Parameters.AddWithValue("@status", trimmedStatus);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(MapRoadmap(reader));
            }
        }

        foreach (var r in list)
        {
            r.Milestones = await FetchMilestonesForRoadmapAsync(connection, r.Id, cancellationToken).ConfigureAwait(false);
        }

        return list.AsReadOnly();
    }

    public async Task AddMilestoneAsync(RoadmapMilestone milestone, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(milestone);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT INTO roadmap_milestones (id, roadmap_id, title, order_index, status, notes, created_at, completed_at)
            VALUES (@id, @roadmap_id, @title, @order_index, @status, @notes, @created_at, @completed_at);
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", milestone.Id);
        cmd.Parameters.AddWithValue("@roadmap_id", milestone.RoadmapId);
        cmd.Parameters.AddWithValue("@title", milestone.Title.Trim());
        cmd.Parameters.AddWithValue("@order_index", milestone.OrderIndex);
        cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(milestone.Status) ? "pending" : milestone.Status.Trim());
        cmd.Parameters.AddWithValue("@notes", (object?)milestone.Notes?.Trim() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@created_at", milestone.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("@completed_at", milestone.CompletedAt.HasValue ? milestone.CompletedAt.Value.ToString("O", CultureInfo.InvariantCulture) : DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateMilestoneStatusAsync(string milestoneId, string status, DateTimeOffset? completedAt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(milestoneId)) return;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            UPDATE roadmap_milestones
            SET status = @status,
                completed_at = @completed_at
            WHERE id = @id;
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", milestoneId);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@completed_at", completedAt.HasValue ? completedAt.Value.ToString("O", CultureInfo.InvariantCulture) : DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoadmapMilestone?> GetMilestoneByIdAsync(string milestoneId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(milestoneId)) return null;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, roadmap_id, title, order_index, status, notes, created_at, completed_at
            FROM roadmap_milestones
            WHERE id = @id
            LIMIT 1;
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", milestoneId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return MapMilestone(reader);
        }

        return null;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = "DELETE FROM roadmaps WHERE id = @id;";
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<RoadmapMilestone>> FetchMilestonesForRoadmapAsync(SqliteConnection connection, string roadmapId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, roadmap_id, title, order_index, status, notes, created_at, completed_at
            FROM roadmap_milestones
            WHERE roadmap_id = @roadmap_id
            ORDER BY order_index ASC;
            """;

        var list = new List<RoadmapMilestone>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@roadmap_id", roadmapId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(MapMilestone(reader));
        }

        return list;
    }

    private static Roadmap MapRoadmap(SqliteDataReader reader)
    {
        var id = reader.GetString(0);
        var goalId = reader.IsDBNull(1) ? null : reader.GetString(1);
        var title = reader.GetString(2);
        var description = reader.IsDBNull(3) ? null : reader.GetString(3);
        var status = reader.GetString(4);
        var createdAtStr = reader.GetString(5);
        var updatedAtStr = reader.GetString(6);

        var createdAt = DateTimeOffset.TryParse(createdAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCreated)
            ? parsedCreated
            : DateTimeOffset.UtcNow;

        var updatedAt = DateTimeOffset.TryParse(updatedAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedUpdated)
            ? parsedUpdated
            : DateTimeOffset.UtcNow;

        return new Roadmap
        {
            Id = id,
            GoalId = goalId,
            Title = title,
            Description = description,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private static RoadmapMilestone MapMilestone(SqliteDataReader reader)
    {
        var id = reader.GetString(0);
        var roadmapId = reader.GetString(1);
        var title = reader.GetString(2);
        var orderIndex = reader.GetInt32(3);
        var status = reader.GetString(4);
        var notes = reader.IsDBNull(5) ? null : reader.GetString(5);
        var createdAtStr = reader.GetString(6);
        var completedAtStr = reader.IsDBNull(7) ? null : reader.GetString(7);

        var createdAt = DateTimeOffset.TryParse(createdAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCreated)
            ? parsedCreated
            : DateTimeOffset.UtcNow;

        DateTimeOffset? completedAt = null;
        if (!string.IsNullOrWhiteSpace(completedAtStr) &&
            DateTimeOffset.TryParse(completedAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCompleted))
        {
            completedAt = parsedCompleted;
        }

        return new RoadmapMilestone
        {
            Id = id,
            RoadmapId = roadmapId,
            Title = title,
            OrderIndex = orderIndex,
            Status = status,
            Notes = notes,
            CreatedAt = createdAt,
            CompletedAt = completedAt
        };
    }
}
