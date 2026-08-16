using ATLAS.Core.Entities;
using ATLAS.Core.Integrations.Gmail;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to list recent emails from Gmail (read-only triage/capture).
/// Does not modify message labels or mark messages as read.
/// </summary>
public class GmailListRecentCommand : ICommand
{
    private readonly IGmailClient _gmailClient;

    public const string CommandId = "gmail.list_recent";

    public GmailListRecentCommand(IGmailClient gmailClient)
    {
        _gmailClient = gmailClient ?? throw new ArgumentNullException(nameof(gmailClient));
    }

    public string Id => CommandId;

    public string Name => "Listar Correos de Gmail";

    public string Description => "Obtiene los últimos N correos de Gmail en modo solo lectura sin modificarlos ni marcarlos como leídos.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("limit", typeof(int), "Cantidad máxima de correos a consultar (1-50)", IsRequired: false, DefaultValue: 10),
        new("query", typeof(string), "Filtro de búsqueda opcional (ej: is:unread, from:alguien@ejemplo.com)", IsRequired: false)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var limit = 10;
        string? query = null;

        if (parameters != null)
        {
            if (parameters.TryGetValue("limit", out var rawLimit) && rawLimit != null)
            {
                if (rawLimit is int l) limit = Math.Clamp(l, 1, 50);
                else if (int.TryParse(rawLimit.ToString(), out var parsedL)) limit = Math.Clamp(parsedL, 1, 50);
            }

            if (parameters.TryGetValue("query", out var rawQuery) && rawQuery != null)
            {
                var q = rawQuery.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(q))
                {
                    query = q;
                }
            }
        }

        try
        {
            var messages = await _gmailClient.ListRecentMessagesAsync(limit, query, cancellationToken).ConfigureAwait(false);

            return CommandResult.Success(messages);
        }
        catch (GmailAuthException ex)
        {
            return CommandResult.Failure(ex.Message);
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Failure("La consulta de correos de Gmail fue cancelada.");
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Error al consultar correos de Gmail: {ex.Message}");
        }
    }
}
