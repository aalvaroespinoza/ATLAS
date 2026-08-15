using System.Globalization;
using ATLAS.Core.Entities;

namespace ATLAS.UI.ViewModels;

public class TransactionItemViewModel
{
    private static readonly CultureInfo ArCulture = new("es-AR");

    public Transaction Transaction { get; }

    public string Description => Transaction.Descripcion;

    public string FormattedAmount
    {
        get
        {
            var formatted = Transaction.Monto.ToString("N2", ArCulture);
            return Transaction.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase)
                ? $"+ ${formatted}"
                : $"- ${formatted}";
        }
    }

    public string FormattedDate => Transaction.Fecha.ToLocalTime().ToString("dd MMM yyyy, HH:mm", ArCulture);

    public string OriginBadge => Transaction.Origen switch
    {
        "mercadopago" => "Mercado Pago",
        "telegram" => "Telegram",
        "launcher" => "Launcher",
        _ => "Manual"
    };

    public string CategoryDisplay => string.IsNullOrWhiteSpace(Transaction.Categoria) ? "Sin categoría" : Transaction.Categoria;

    public TransactionItemViewModel(Transaction transaction)
    {
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }
}
