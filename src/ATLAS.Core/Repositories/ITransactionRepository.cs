using ATLAS.Core.Entities;

namespace ATLAS.Core.Repositories;

/// <summary>
/// Repository contract for Transaction persistence and retrieval.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Persists a new transaction into storage.
    /// </summary>
    Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by its unique identifier.
    /// </summary>
    Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by its external ID (e.g. Mercado Pago payment ID).
    /// </summary>
    Task<Transaction?> GetByExternalIdAsync(string idExterno, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent transactions ordered by date descending.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a batch of transactions idempotently (ignoring duplicates matching id_externo).
    /// </summary>
    Task<int> CreateBatchAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the category and optional subcategory of an existing transaction.
    /// </summary>
    Task<bool> UpdateCategoryAsync(string id, string? categoria, string? subcategoria = null, CancellationToken cancellationToken = default);
}
