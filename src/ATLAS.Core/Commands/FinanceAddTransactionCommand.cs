using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Events;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to manually create a new financial transaction (expense, income, transfer) in ATLAS.
/// </summary>
public class FinanceAddTransactionCommand : ICommand
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAtlasEventBus? _eventBus;

    public const string CommandId = "finance.add_transaction";

    public FinanceAddTransactionCommand(ITransactionRepository transactionRepository, IAtlasEventBus? eventBus = null)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _eventBus = eventBus;
    }

    public string Id => CommandId;

    public string Name => "Registrar Transacción";

    public string Description => "Registra un gasto o ingreso manual en el sistema de finanzas.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("amount", typeof(decimal), "Monto numérico de la transacción", IsRequired: true),
        new("description", typeof(string), "Detalle o concepto del gasto/ingreso", IsRequired: true),
        new("category", typeof(string), "Categoría opcional (ej: Comida, Servicios, Transporte)", IsRequired: false),
        new("type", typeof(string), "Tipo de movimiento ('expense', 'income', 'transfer')", IsRequired: false, DefaultValue: "expense"),
        new("origin", typeof(string), "Origen del registro ('manual', 'telegram', 'launcher', 'mercadopago')", IsRequired: false, DefaultValue: "manual"),
        new("currency", typeof(string), "Código de moneda ('ARS', 'USD', etc.)", IsRequired: false, DefaultValue: "ARS"),
        new("date", typeof(string), "Fecha y hora del movimiento (ISO-8601, opcional)", IsRequired: false)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null)
        {
            return CommandResult.Failure("Faltan parámetros requeridos para registrar la transacción.");
        }

        // 1. Amount validation
        if (!parameters.TryGetValue("amount", out var rawAmount) || rawAmount == null)
        {
            return CommandResult.Failure("El parámetro 'amount' es obligatorio.");
        }

        decimal amount;
        if (rawAmount is decimal d)
        {
            amount = d;
        }
        else if (rawAmount is double dbl)
        {
            amount = (decimal)dbl;
        }
        else if (rawAmount is int i)
        {
            amount = i;
        }
        else if (rawAmount is long l)
        {
            amount = l;
        }
        else if (!decimal.TryParse(rawAmount.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
        {
            return CommandResult.Failure("El valor de 'amount' no es un número válido.");
        }

        if (amount <= 0)
        {
            return CommandResult.Failure("El monto de la transacción debe ser un valor positivo mayor a 0.");
        }

        // 2. Description validation
        if (!parameters.TryGetValue("description", out var rawDesc) ||
            rawDesc == null ||
            string.IsNullOrWhiteSpace(rawDesc.ToString()))
        {
            return CommandResult.Failure("El parámetro 'description' es obligatorio.");
        }

        var description = rawDesc.ToString()!.Trim();

        // 3. Optional parameters
        string? category = null;
        if (parameters.TryGetValue("category", out var rawCat) && rawCat != null)
        {
            var catStr = rawCat.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(catStr)) category = catStr;
        }

        var type = "expense";
        if (parameters.TryGetValue("type", out var rawType) && rawType != null)
        {
            var typeStr = rawType.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(typeStr)) type = typeStr;
        }

        var origin = "manual";
        if (parameters.TryGetValue("origin", out var rawOrigin) && rawOrigin != null)
        {
            var originStr = rawOrigin.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(originStr)) origin = originStr;
        }

        var currency = "ARS";
        if (parameters.TryGetValue("currency", out var rawCurr) && rawCurr != null)
        {
            var currStr = rawCurr.ToString()?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(currStr)) currency = currStr;
        }

        var fecha = DateTimeOffset.UtcNow;
        if (parameters.TryGetValue("date", out var rawDate) && rawDate != null)
        {
            if (rawDate is DateTimeOffset dto)
            {
                fecha = dto;
            }
            else if (rawDate is DateTime dt)
            {
                fecha = new DateTimeOffset(dt);
            }
            else if (DateTimeOffset.TryParse(rawDate.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                fecha = parsedDate;
            }
        }

        var transaction = new Transaction
        {
            Fecha = fecha,
            Monto = amount,
            Tipo = type,
            Origen = origin,
            Descripcion = description,
            Moneda = currency,
            Categoria = category,
            Estado = "approved",
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var created = await _transactionRepository.CreateAsync(transaction, cancellationToken).ConfigureAwait(false);

            if (_eventBus != null)
            {
                await _eventBus.PublishAsync(new TransactionCreatedEvent(
                    TransactionId: created.Id,
                    Description: created.Descripcion,
                    Amount: created.Monto,
                    Type: created.Tipo,
                    Category: created.Categoria,
                    Source: created.Origen,
                    EventId: Guid.NewGuid().ToString("N"),
                    OccurredAt: created.Fecha
                ), cancellationToken).ConfigureAwait(false);
            }

            return CommandResult.Success(created);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}
