using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Storage.Database;
using Microsoft.Data.Sqlite;

namespace ATLAS.Storage.Repositories;

public class ActivitiesRepository : IActivityRepository
{
    private readonly string _connectionString;

    public ActivitiesRepository(string? connectionString = null)
    {
        _connectionString = connectionString ?? DatabaseConfig.GetDefaultConnectionString();
    }

    public async Task<ActivityRecord> CreateAsync(ActivityRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT INTO activities (id, type, source_id, title, summary, relevance_score, timestamp)
            VALUES (@id, @type, @source_id, @title, @summary, @relevance_score, @timestamp);
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", record.Id);
        command.Parameters.AddWithValue("@type", record.Type);
        command.Parameters.AddWithValue("@source_id", (object?)record.SourceId ?? DBNull.Value);
        command.Parameters.AddWithValue("@title", record.Title);
        command.Parameters.AddWithValue("@summary", (object?)record.Summary ?? DBNull.Value);
        command.Parameters.AddWithValue("@relevance_score", record.RelevanceScore);
        command.Parameters.AddWithValue("@timestamp", record.Timestamp.ToString("O", CultureInfo.InvariantCulture));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return record;
    }

    public async Task<IReadOnlyList<ActivityRecord>> GetRecentAsync(int minRelevance = 0, int count = 20, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, type, source_id, title, summary, relevance_score, timestamp
            FROM activities
            WHERE relevance_score >= @min
            ORDER BY datetime(timestamp) DESC
            LIMIT @limit;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@min", minRelevance);
        command.Parameters.AddWithValue("@limit", count);

        var activities = new List<ActivityRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetString(0);
            var type = reader.GetString(1);
            var sourceId = reader.IsDBNull(2) ? null : reader.GetString(2);
            var title = reader.GetString(3);
            var summary = reader.IsDBNull(4) ? null : reader.GetString(4);
            var relevance = reader.GetInt32(5);
            var timestampStr = reader.GetString(6);

            var timestamp = DateTimeOffset.TryParse(timestampStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDt)
                ? parsedDt
                : DateTimeOffset.UtcNow;

            activities.Add(new ActivityRecord
            {
                Id = id,
                Type = type,
                SourceId = sourceId,
                Title = title,
                Summary = summary,
                RelevanceScore = relevance,
                Timestamp = timestamp
            });
        }

        return activities.AsReadOnly();
    }
}
