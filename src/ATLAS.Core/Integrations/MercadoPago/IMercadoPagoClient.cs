using ATLAS.Core.Entities;

namespace ATLAS.Core.Integrations.MercadoPago;

/// <summary>
/// Client interface for interacting with Mercado Pago API (read-only transactions synchronization).
/// </summary>
public interface IMercadoPagoClient
{
    /// <summary>
    /// Fetches recent payment transactions from Mercado Pago API using the configured Access Token.
    /// </summary>
    Task<IReadOnlyList<Transaction>> FetchRecentTransactionsAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity and token validity against Mercado Pago.
    /// </summary>
    Task<(bool Success, string Message, TimeSpan Latency)> PingAsync(CancellationToken cancellationToken = default);
}
