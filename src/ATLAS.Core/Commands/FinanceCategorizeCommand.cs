using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Ai;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to suggest a financial category for a transaction using IAiProvider (Gemini).
/// Does not automatically overwrite the stored category, returning the suggestion for user confirmation.
/// </summary>
public class FinanceCategorizeCommand : ICommand
{
    public const string CommandId = "finance.categorize";

    private readonly ITransactionRepository _transactionRepository;
    private readonly IAiProvider _aiProvider;

    public string Id => CommandId;
    public string Name => "Categorizar Transacción con IA";
    public string Description => "Analiza la descripción del movimiento y sugiere una categoría adecuada usando IA.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema => new[]
    {
        new CommandParameterDescriptor(
            Name: "transaction_id",
            ParameterType: typeof(string),
            Description: "ID único de la transacción a categorizar.",
            IsRequired: true
        )
    };

    public FinanceCategorizeCommand(ITransactionRepository transactionRepository, IAiProvider aiProvider)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null ||
            !parameters.TryGetValue("transaction_id", out var idObj) ||
            idObj is not string transactionId ||
            string.IsNullOrWhiteSpace(transactionId))
        {
            return CommandResult.Failure("El parámetro 'transaction_id' es requerido.");
        }

        try
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId.Trim(), cancellationToken);
            if (transaction == null)
            {
                return CommandResult.Failure($"No se encontró la transacción con ID '{transactionId}'.");
            }

            if (string.IsNullOrWhiteSpace(transaction.Descripcion))
            {
                return CommandResult.Failure("La transacción no contiene una descripción para analizar.");
            }

            var prompt = $"""
            Dada la siguiente descripción de un movimiento financiero o gasto: "{transaction.Descripcion}" (monto: {transaction.Monto.ToString("N0", CultureInfo.InvariantCulture)} {transaction.Moneda}, tipo: {transaction.Tipo}).
            Sugerí una única categoría concisa, estándar y clara en español (por ejemplo: Comida, Supermercado, Servicios, Transporte, Vivienda, Salud, Educación, Entretenimiento, Compras, Salario, Transferencias, Inversiones, Impuestos, Otro).
            Respondé ÚNICAMENTE con el nombre de la categoría, en una sola palabra o frase corta, sin explicaciones ni markdown.
            """;

            var aiResponse = await _aiProvider.AskAsync(prompt, cancellationToken);
            if (string.IsNullOrWhiteSpace(aiResponse))
            {
                return CommandResult.Failure("El proveedor de IA no devolvió una sugerencia válida.");
            }

            var cleanedCategory = CleanCategorySuggestion(aiResponse);

            return CommandResult.Success(cleanedCategory);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Error al categorizar la transacción con IA: {ex.Message}");
        }
    }

    private static string CleanCategorySuggestion(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // Take first non-empty line
        var lines = raw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var clean = lines.Length > 0 ? lines[0] : string.Empty;

        // Remove markdown formatting like **Category**, `Category`, quotes, bullets, punctuation
        clean = Regex.Replace(clean, @"^[\*\#\`\'\""\-\s\>\•\d\.\:]+|[\*\#\`\'\""\-\s\.\:]+$", "");
        clean = clean.Trim('*', '`', '#', '"', '\'', '-', '.', ':', ' ', '\t', ';', ',');

        // Capitalize first letter
        if (clean.Length > 0)
        {
            clean = char.ToUpper(clean[0], CultureInfo.CurrentCulture) + (clean.Length > 1 ? clean[1..] : string.Empty);
        }

        return clean;
    }
}
