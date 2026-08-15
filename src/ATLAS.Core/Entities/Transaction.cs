namespace ATLAS.Core.Entities;

/// <summary>
/// Domain model for a financial transaction (expense, income, transfer) in ATLAS Personal OS.
/// </summary>
public class Transaction
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Fecha { get; init; } = DateTimeOffset.UtcNow;
    public decimal Monto { get; init; }
    public string Tipo { get; init; } = "expense"; // "expense", "income", "transfer"
    public string Origen { get; init; } = "manual"; // "manual", "telegram", "launcher", "mercadopago"
    public string Descripcion { get; init; } = string.Empty;
    public string Moneda { get; init; } = "ARS";
    public string? Categoria { get; init; }
    public string? Subcategoria { get; init; }
    public string? IdExterno { get; init; }
    public string Estado { get; init; } = "approved"; // "approved", "pending", "rejected"
    public string? Metadata { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
