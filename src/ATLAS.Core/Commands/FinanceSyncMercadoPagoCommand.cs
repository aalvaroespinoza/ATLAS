using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to synchronize recent financial transactions from Mercado Pago API using a personal access token.
/// </summary>
public class FinanceSyncMercadoPagoCommand : ICommand
{
    private readonly ISecretVault _secretVault;
    private readonly ITransactionRepository _transactionRepository;
    private readonly HttpClient _httpClient;

    public const string CommandId = "finance.sync_mercadopago";

    public FinanceSyncMercadoPagoCommand(
        ISecretVault secretVault,
        ITransactionRepository transactionRepository,
        HttpClient? httpClient = null)
    {
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _httpClient = httpClient ?? new HttpClient();
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
        var token = _secretVault.GetSecret(SecretKeys.MercadoPagoAccessToken)?.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return CommandResult.Failure("El Access Token de Mercado Pago no está configurado en Configuración.");
        }

        var limit = 50;
        if (parameters != null && parameters.TryGetValue("limit", out var rawLimit) && rawLimit != null)
        {
            if (rawLimit is int l) limit = Math.Clamp(l, 1, 100);
            else if (int.TryParse(rawLimit.ToString(), out var parsedL)) limit = Math.Clamp(parsedL, 1, 100);
        }

        var endpoint = $"https://api.mercadopago.com/v1/payments/search?sort=date_created&criteria=desc&limit={limit}";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return CommandResult.Failure($"Fallo de red al conectar con Mercado Pago: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Failure("La sincronización de Mercado Pago fue cancelada.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return CommandResult.Failure($"Mercado Pago API respondió con error ({response.StatusCode}): {json}");
        }

        try
        {
            var transactions = ParseMercadoPagoPaymentsJson(json);
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
        catch (Exception ex)
        {
            return CommandResult.Failure($"Error al procesar los datos de Mercado Pago: {ex.Message}");
        }
    }

    public static IReadOnlyList<Transaction> ParseMercadoPagoPaymentsJson(string json)
    {
        var list = new List<Transaction>();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("results", out var resultsArr) || resultsArr.ValueKind != JsonValueKind.Array)
        {
            return list;
        }

        foreach (var item in resultsArr.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idProp))
            {
                continue;
            }

            var idExterno = idProp.ToString();
            if (string.IsNullOrWhiteSpace(idExterno)) continue;

            // Date
            DateTimeOffset fecha = DateTimeOffset.UtcNow;
            if (item.TryGetProperty("date_approved", out var dateApprovedProp) && dateApprovedProp.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(dateApprovedProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dApp))
                {
                    fecha = dApp;
                }
            }
            else if (item.TryGetProperty("date_created", out var dateCreatedProp) && dateCreatedProp.ValueKind == JsonValueKind.String)
            {
                if (DateTimeOffset.TryParse(dateCreatedProp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dCre))
                {
                    fecha = dCre;
                }
            }

            // Amount
            decimal monto = 0;
            if (item.TryGetProperty("transaction_amount", out var amountProp) && amountProp.TryGetDecimal(out var parsedMonto))
            {
                monto = parsedMonto;
            }

            if (monto <= 0)
            {
                continue;
            }

            // Description
            var descripcion = "Pago Mercado Pago";
            if (item.TryGetProperty("description", out var descProp) && !string.IsNullOrWhiteSpace(descProp.GetString()))
            {
                descripcion = descProp.GetString()!.Trim();
            }
            else if (item.TryGetProperty("reason", out var reasonProp) && !string.IsNullOrWhiteSpace(reasonProp.GetString()))
            {
                descripcion = reasonProp.GetString()!.Trim();
            }
            else if (item.TryGetProperty("statement_descriptor", out var stmProp) && !string.IsNullOrWhiteSpace(stmProp.GetString()))
            {
                descripcion = stmProp.GetString()!.Trim();
            }

            // Currency
            var moneda = "ARS";
            if (item.TryGetProperty("currency_id", out var currProp) && !string.IsNullOrWhiteSpace(currProp.GetString()))
            {
                moneda = currProp.GetString()!.Trim().ToUpperInvariant();
            }

            // Status
            var estado = "approved";
            if (item.TryGetProperty("status", out var statusProp) && !string.IsNullOrWhiteSpace(statusProp.GetString()))
            {
                estado = statusProp.GetString()!.Trim();
            }

            // Type
            var tipo = "expense";
            if (item.TryGetProperty("operation_type", out var opTypeProp))
            {
                var opType = opTypeProp.GetString();
                if (opType != null && opType.Equals("money_transfer_received", StringComparison.OrdinalIgnoreCase))
                {
                    tipo = "income";
                }
            }

            // Metadata summary
            string? metadataJson = null;
            try
            {
                var metadataDict = new Dictionary<string, object?>
                {
                    ["mp_id"] = idExterno,
                    ["status_detail"] = item.TryGetProperty("status_detail", out var sd) ? sd.GetString() : null,
                    ["payment_type_id"] = item.TryGetProperty("payment_type_id", out var pt) ? pt.GetString() : null,
                    ["payment_method_id"] = item.TryGetProperty("payment_method_id", out var pm) ? pm.GetString() : null
                };
                metadataJson = JsonSerializer.Serialize(metadataDict);
            }
            catch
            {
                // Ignore metadata serialization errors
            }

            list.Add(new Transaction
            {
                Id = Guid.NewGuid().ToString("N"),
                Fecha = fecha,
                Monto = monto,
                Tipo = tipo,
                Origen = "mercadopago",
                Descripcion = descripcion,
                Moneda = moneda,
                Categoria = null, // Explicitly null per specification
                Subcategoria = null,
                IdExterno = idExterno,
                Estado = estado,
                Metadata = metadataJson,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return list;
    }
}
