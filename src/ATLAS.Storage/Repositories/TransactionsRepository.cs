using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Storage.Database;
using Microsoft.Data.Sqlite;

namespace ATLAS.Storage.Repositories;

/// <summary>
/// SQLite implementation of ITransactionRepository.
/// </summary>
public class TransactionsRepository : ITransactionRepository
{
    private readonly string _connectionString;

    public TransactionsRepository(string? connectionString = null)
    {
        _connectionString = connectionString ?? DatabaseConfig.GetDefaultConnectionString();
    }

    public async Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (string.IsNullOrWhiteSpace(transaction.Id))
        {
            throw new ArgumentException("Transaction ID cannot be null or whitespace.", nameof(transaction));
        }

        if (string.IsNullOrWhiteSpace(transaction.Descripcion))
        {
            throw new ArgumentException("Transaction Descripcion cannot be null or whitespace.", nameof(transaction));
        }

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT INTO transactions (
                id, fecha, monto, tipo, origen, descripcion, moneda, categoria, subcategoria, id_externo, estado, metadata, created_at
            ) VALUES (
                @id, @fecha, @monto, @tipo, @origen, @descripcion, @moneda, @categoria, @subcategoria, @id_externo, @estado, @metadata, @created_at
            );
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        PopulateCommandParameters(command, transaction);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return transaction;
    }

    public async Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, fecha, monto, tipo, origen, descripcion, moneda, categoria, subcategoria, id_externo, estado, metadata, created_at
            FROM transactions
            WHERE id = @id
            LIMIT 1;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return MapTransaction(reader);
        }

        return null;
    }

    public async Task<Transaction?> GetByExternalIdAsync(string idExterno, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idExterno)) return null;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, fecha, monto, tipo, origen, descripcion, moneda, categoria, subcategoria, id_externo, estado, metadata, created_at
            FROM transactions
            WHERE id_externo = @id_externo
            LIMIT 1;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id_externo", idExterno.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return MapTransaction(reader);
        }

        return null;
    }

    public async Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            SELECT id, fecha, monto, tipo, origen, descripcion, moneda, categoria, subcategoria, id_externo, estado, metadata, created_at
            FROM transactions
            ORDER BY datetime(fecha) DESC
            LIMIT @limit;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@limit", Math.Clamp(limit, 1, 500));

        var transactions = new List<Transaction>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            transactions.Add(MapTransaction(reader));
        }

        return transactions.AsReadOnly();
    }

    public async Task<int> CreateBatchAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            INSERT OR IGNORE INTO transactions (
                id, fecha, monto, tipo, origen, descripcion, moneda, categoria, subcategoria, id_externo, estado, metadata, created_at
            ) VALUES (
                @id, @fecha, @monto, @tipo, @origen, @descripcion, @moneda, @categoria, @subcategoria, @id_externo, @estado, @metadata, @created_at
            );
            """;

        var insertedCount = 0;
        foreach (var item in transactions)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (string.IsNullOrWhiteSpace(item.Id) || string.IsNullOrWhiteSpace(item.Descripcion)) continue;

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            PopulateCommandParameters(command, item);

            var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            insertedCount += rows;
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return insertedCount;
    }

    public async Task<bool> UpdateCategoryAsync(string id, string? categoria, string? subcategoria = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        const string sql = """
            UPDATE transactions
            SET categoria = @categoria,
                subcategoria = @subcategoria
            WHERE id = @id;
            """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id.Trim());
        command.Parameters.AddWithValue("@categoria", (object?)categoria?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@subcategoria", (object?)subcategoria?.Trim() ?? DBNull.Value);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        return rowsAffected > 0;
    }

    private static void PopulateCommandParameters(SqliteCommand command, Transaction transaction)
    {
        command.Parameters.AddWithValue("@id", transaction.Id);
        command.Parameters.AddWithValue("@fecha", transaction.Fecha.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@monto", (double)transaction.Monto);
        command.Parameters.AddWithValue("@tipo", string.IsNullOrWhiteSpace(transaction.Tipo) ? "expense" : transaction.Tipo.Trim());
        command.Parameters.AddWithValue("@origen", string.IsNullOrWhiteSpace(transaction.Origen) ? "manual" : transaction.Origen.Trim());
        command.Parameters.AddWithValue("@descripcion", transaction.Descripcion.Trim());
        command.Parameters.AddWithValue("@moneda", string.IsNullOrWhiteSpace(transaction.Moneda) ? "ARS" : transaction.Moneda.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("@categoria", (object?)transaction.Categoria?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@subcategoria", (object?)transaction.Subcategoria?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@id_externo", (object?)transaction.IdExterno?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(transaction.Estado) ? "approved" : transaction.Estado.Trim());
        command.Parameters.AddWithValue("@metadata", (object?)transaction.Metadata?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@created_at", transaction.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
    }

    private static Transaction MapTransaction(SqliteDataReader reader)
    {
        var id = reader.GetString(0);
        var fechaStr = reader.GetString(1);
        var monto = (decimal)reader.GetDouble(2);
        var tipo = reader.GetString(3);
        var origen = reader.GetString(4);
        var descripcion = reader.GetString(5);
        var moneda = reader.GetString(6);
        var categoria = reader.IsDBNull(7) ? null : reader.GetString(7);
        var subcategoria = reader.IsDBNull(8) ? null : reader.GetString(8);
        var idExterno = reader.IsDBNull(9) ? null : reader.GetString(9);
        var estado = reader.GetString(10);
        var metadata = reader.IsDBNull(11) ? null : reader.GetString(11);
        var createdAtStr = reader.GetString(12);

        var fecha = DateTimeOffset.TryParse(fechaStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedFecha)
            ? parsedFecha
            : DateTimeOffset.UtcNow;

        var createdAt = DateTimeOffset.TryParse(createdAtStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedCreated)
            ? parsedCreated
            : DateTimeOffset.UtcNow;

        return new Transaction
        {
            Id = id,
            Fecha = fecha,
            Monto = monto,
            Tipo = tipo,
            Origen = origen,
            Descripcion = descripcion,
            Moneda = moneda,
            Categoria = categoria,
            Subcategoria = subcategoria,
            IdExterno = idExterno,
            Estado = estado,
            Metadata = metadata,
            CreatedAt = createdAt
        };
    }
}
