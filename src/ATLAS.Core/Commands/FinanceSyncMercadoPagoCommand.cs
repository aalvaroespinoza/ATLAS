using ATLAS.Core.Entities;
using ATLAS.Core.Integrations;
using ATLAS.Core.Integrations.MercadoPago;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to synchronize recent financial transactions from Mercado Pago API via IMercadoPagoClient.
/// </summary>
public class FinanceSyncMercadoPagoCommand : ICommand
{
    private readonly IMercadoPagoClient _mercadoPagoClient;
    private readonly ITransactionRepository _transactionRepository;

    public const string CommandId = "finance.sync_mercadopago";

    public FinanceSyncMercadoPagoCommand(
        IMercadoPagoClient mercadoPagoClient,
        ITransactionRepository transactionRepository)
    {
        _mercadoPagoClient = mercadoPagoClient ?? throw new ArgumentNullException(nameof(mercadoPagoClient));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    /// <summary>
    /// Backward-compatible constructor for testing and factory initializations.
    /// </summary>
    public FinanceSyncMercadoPagoCommand(
        ISecretVault secretVault,
        ITransactionRepository transactionRepository,
        HttpClient? httpClient = null)
        : this(new MercadoPagoClient(httpClient ?? new HttpClient(), secretVault), transactionRepository)
    {
    }

    public string Id => CommandId;

    public string Name => "Sincronizar Mercado Pago";

    public string Description => "Descarga movimientos recientes de Mercado Pago y los normaliza a transacciones de ATLAS.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("limit", typeof(int), "Cantidad máxima de movimientos a descargar (1-100)", IsRequired: false, DefaultValue: 50)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var limit = 50;
        if (parameters != null && parameters.TryGetValue("limit", out var rawLimit) && rawLimit != null)
        {
            if (rawLimit is int l) limit = Math.Clamp(l, 1, 100);
            else if (int.TryParse(rawLimit.ToString(), out var parsedL)) limit = Math.Clamp(parsedL, 1, 100);
        }

        try
        {
            var transactions = await _mercadoPagoClient.FetchRecentTransactionsAsync(limit, cancellationToken).ConfigureAwait(false);

            if (transactions.Count == 0)
            {
                return CommandResult.Success(new
                {
                    TotalFetched = 0,
                    NewInserted = 0,
                    Message = "No se encontraron movimientos recientes en Mercado Pago."
                });
            }

            var insertedCount = await _transactionRepository.CreateBatchAsync(transactions, cancellationToken).ConfigureAwait(false);

            return CommandResult.Success(new
            {
                TotalFetched = transactions.Count,
                NewInserted = insertedCount,
                Message = $"Sincronización completada: {insertedCount} transacciones nuevas guardadas ({transactions.Count} obtenidas)."
            });
        }
        catch (IntegrationException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Failure("La sincronización de Mercado Pago fue cancelada.");
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Error al procesar los datos de Mercado Pago: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method forwarded to MercadoPagoClient.ParseMercadoPagoPaymentsJson for backward compatibility.
    /// </summary>
    public static IReadOnlyList<Transaction> ParseMercadoPagoPaymentsJson(string json)
        => MercadoPagoClient.ParseMercadoPagoPaymentsJson(json);
}
