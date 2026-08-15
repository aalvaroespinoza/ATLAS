using System.Globalization;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Finance;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Integrations.Telegram;

/// <summary>
/// Processes incoming Telegram messages and maps them to ATLAS Core Commands, returning confirmation text.
/// </summary>
public class TelegramMessageProcessor
{
    private static readonly CultureInfo ArCulture = new("es-AR");
    private readonly ICommandRegistry _commandRegistry;
    private readonly IHabitRepository _habitRepository;

    public TelegramMessageProcessor(ICommandRegistry commandRegistry, IHabitRepository habitRepository)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
    }

    /// <summary>
    /// Interprets message text, dispatches corresponding command, and returns the response message to send back to the user.
    /// </summary>
    public async Task<string> ProcessMessageAsync(TelegramMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var text = message.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return "⚠️ Mensaje vacío.";
        }

        // 1. Help / Start commands
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            return "👋 Bot de ATLAS conectado.\n\n" +
                   "• Enviá cualquier texto para capturarlo como nota.\n" +
                   "• /habit <nombre> para completar un hábito.\n" +
                   "• /done <nombre> para completar un hábito.\n" +
                   "• /expense <monto> <descripción> para registrar un gasto.";
        }

        // 2. Expense / Transaction commands (/expense or /gasto)
        if (text.StartsWith("/expense", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("/gasto", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("!expense", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("!gasto", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleExpenseAsync(text, cancellationToken).ConfigureAwait(false);
        }

        // 3. Habit Complete commands (/habit or /done)
        if (text.StartsWith("/habit", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("/done", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("!done", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleHabitCompleteAsync(text, cancellationToken).ConfigureAwait(false);
        }

        // 4. Free text -> Capture Note
        return await HandleCaptureNoteAsync(text, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> HandleExpenseAsync(string input, CancellationToken cancellationToken)
    {
        if (!ExpenseTextParser.TryParse(input, out var amount, out var description))
        {
            return "⚠️ Formato inválido. Usá: /expense <monto> <descripción> (ej: /expense 4500 Supermercado).";
        }

        var parameters = new Dictionary<string, object?>
        {
            ["amount"] = amount,
            ["description"] = description,
            ["origin"] = "telegram",
            ["type"] = "expense",
            ["currency"] = "ARS"
        };

        var result = await _commandRegistry.ExecuteAsync(FinanceAddTransactionCommand.CommandId, parameters, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            var formattedAmount = amount.ToString("N0", ArCulture);
            return $"✓ Gasto de ${formattedAmount} registrado: {description}.";
        }

        return $"⚠️ Error al registrar gasto: {result.ErrorMessage}";
    }

    private async Task<string> HandleHabitCompleteAsync(string input, CancellationToken cancellationToken)
    {
        var rawQuery = StripPrefix(input, "/habit", "/done", "!done");
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return "⚠️ Por favor indicá el nombre del hábito (ej: /habit agua).";
        }

        string habitQuery = rawQuery;
        string? note = "vía Telegram";

        if (rawQuery.Contains(':'))
        {
            var parts = rawQuery.Split(':', 2, StringSplitOptions.TrimEntries);
            habitQuery = parts[0];
            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                note = parts[1];
            }
        }

        var allHabits = await _habitRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var matchedHabit = allHabits.FirstOrDefault(h =>
            h.Name.Equals(habitQuery, StringComparison.CurrentCultureIgnoreCase) ||
            h.Name.Contains(habitQuery, StringComparison.CurrentCultureIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(h.Description) && h.Description.Contains(habitQuery, StringComparison.CurrentCultureIgnoreCase)));

        if (matchedHabit == null)
        {
            return $"⚠️ No se encontró ningún hábito que coincida con '{habitQuery}'.";
        }

        var parameters = new Dictionary<string, object?>
        {
            ["habit_id"] = matchedHabit.Id,
            ["note"] = note
        };

        var result = await _commandRegistry.ExecuteAsync(HabitCompleteCommand.CommandId, parameters, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return $"✓ Hábito '{matchedHabit.Name}' completado.";
        }

        return $"⚠️ Error al completar hábito: {result.ErrorMessage}";
    }

    private async Task<string> HandleCaptureNoteAsync(string content, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["content"] = content,
            ["source"] = "telegram",
            ["type"] = "note"
        };

        var result = await _commandRegistry.ExecuteAsync(CaptureNoteCommand.CommandId, parameters, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return "✓ Nota guardada en ATLAS.";
        }

        return $"⚠️ Error al guardar nota: {result.ErrorMessage}";
    }

    private static string StripPrefix(string text, params string[] prefixes)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var trimmed = text.TrimStart();
        foreach (var prefix in prefixes)
        {
            if (trimmed.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[(prefix.Length + 1)..].Trim();
            }
            if (trimmed.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
        }
        return trimmed;
    }
}
