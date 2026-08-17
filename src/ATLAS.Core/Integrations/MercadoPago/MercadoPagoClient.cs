using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using ATLAS.Core.Entities;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.MercadoPago;

/// <summary>
/// Concrete implementation of IMercadoPagoClient handling HTTP communication, bearer tokens, and JSON parsing.
/// </summary>
public sealed class MercadoPagoClient : IMercadoPagoClient
{
    private readonly HttpClient _httpClient;
    private readonly ISecretVault _secretVault;

    public MercadoPagoClient(HttpClient httpClient, ISecretVault secretVault)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
    }

    public async Task<IReadOnlyList<Transaction>> FetchRecentTransactionsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        var token = _secretVault.GetSecret(SecretKeys.MercadoPagoAccessToken)?.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new IntegrationException("mercadopago", "El Access Token de Mercado Pago no está configurado.");
        }

        var clampedLimit = Math.Clamp(limit, 1, 100);
        var endpoint = $"https://api.mercadopago.com/v1/payments/search?sort=date_created&criteria=desc&limit={clampedLimit}";

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
            throw new IntegrationException("mercadopago", $"Fallo de red al conectar con Mercado Pago: {ex.Message}", ex);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new IntegrationException("mercadopago", $"Mercado Pago API respondió con error ({response.StatusCode}): {json}");
        }

        return ParseMercadoPagoPaymentsJson(json);
    }

    public async Task<(bool Success, string Message, TimeSpan Latency)> PingAsync(CancellationToken cancellationToken = default)
    {
        var token = _secretVault.GetSecret(SecretKeys.MercadoPagoAccessToken)?.Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return (false, "Access Token no configurado.", TimeSpan.Zero);
        }

        var sw = Stopwatch.StartNew();
        var endpoint = "https://api.mercadopago.com/v1/payments/search?limit=1";

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                return (true, "Conexión exitosa con Mercado Pago API.", sw.Elapsed);
            }

            return (false, $"Mercado Pago API retornó estado {(int)response.StatusCode} ({response.ReasonPhrase})", sw.Elapsed);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return (false, $"Error de red: {ex.Message}", sw.Elapsed);
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
                Categoria = null,
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
