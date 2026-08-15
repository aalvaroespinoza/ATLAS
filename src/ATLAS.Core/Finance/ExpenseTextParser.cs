using System.Globalization;

namespace ATLAS.Core.Finance;

/// <summary>
/// Parser helper to extract numerical amount and textual description from text commands (Launcher, Telegram).
/// </summary>
public static class ExpenseTextParser
{
    private static readonly string[] ExpensePrefixes = ["/expense", "/gasto", "!expense", "!gasto", "gasto"];

    /// <summary>
    /// Attempts to parse an expense command string into amount and description.
    /// </summary>
    public static bool TryParse(string? rawText, out decimal amount, out string description)
    {
        amount = 0;
        description = string.Empty;

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        var cleaned = StripPrefixes(rawText.Trim());
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return false;
        }

        // Split on whitespace into first token (amount) and remaining tokens (description)
        var parts = cleaned.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        var rawAmountToken = parts[0].Trim();
        if (!TryParseAmount(rawAmountToken, out amount))
        {
            return false;
        }

        description = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
            ? parts[1].Trim()
            : "Gasto";

        return true;
    }

    public static bool TryParseAmount(string token, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var clean = token.TrimStart('$').Trim();

        // Check if contains both dot and comma
        if (clean.Contains('.') && clean.Contains(','))
        {
            var lastDot = clean.LastIndexOf('.');
            var lastComma = clean.LastIndexOf(',');
            if (lastComma > lastDot)
            {
                // Format: 1.250,50 (es-AR / EUR) -> remove dots, replace comma with dot
                clean = clean.Replace(".", "").Replace(',', '.');
            }
            else
            {
                // Format: 1,250.50 (US) -> remove commas
                clean = clean.Replace(",", "");
            }
        }
        else if (clean.Contains('.'))
        {
            var dotParts = clean.Split('.');
            if (dotParts.Length > 2)
            {
                // Format: 1.000.000
                clean = clean.Replace(".", "");
            }
            else if (dotParts.Length == 2 && dotParts[1].Length == 3 && int.TryParse(dotParts[0], out _) && int.TryParse(dotParts[1], out _))
            {
                // Single dot with exactly 3 digits (e.g. 1.250, 10.000) -> thousands dot
                clean = clean.Replace(".", "");
            }
        }
        else if (clean.Contains(','))
        {
            var commaParts = clean.Split(',');
            if (commaParts.Length > 2)
            {
                // Format: 1,000,000
                clean = clean.Replace(",", "");
            }
            else if (commaParts.Length == 2 && commaParts[1].Length == 3 && int.TryParse(commaParts[0], out _) && int.TryParse(commaParts[1], out _))
            {
                // Single comma with exactly 3 digits (e.g. 1,250) -> thousands comma
                clean = clean.Replace(",", "");
            }
            else
            {
                // Decimal comma (e.g. 1250,50 or 15,5) -> replace with dot
                clean = clean.Replace(',', '.');
            }
        }

        return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out amount) && amount > 0;
    }

    private static string StripPrefixes(string text)
    {
        foreach (var prefix in ExpensePrefixes)
        {
            if (text.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                return text[(prefix.Length + 1)..].Trim();
            }
            if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
        }
        return text;
    }
}
