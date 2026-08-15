using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to search notes across title, content, and tags using LIKE pattern matching.
/// </summary>
public class KnowledgeSearchCommand : ICommand
{
    private readonly INoteRepository _noteRepository;

    public const string CommandId = "knowledge.search";

    public KnowledgeSearchCommand(INoteRepository noteRepository)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
    }

    public string Id => CommandId;

    public string Name => "Buscar Conocimiento";

    public string Description => "Busca notas por coincidencia en título, contenido o etiquetas (ordenadas por fecha descendente).";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("query", typeof(string), "Texto a buscar en título, contenido o tags", IsRequired: false, DefaultValue: ""),
        new("limit", typeof(int), "Cantidad máxima de resultados (por defecto 20)", IsRequired: false, DefaultValue: 20)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        string? query = null;
        int limit = 20;

        if (parameters != null)
        {
            if (parameters.TryGetValue("query", out var rawQuery) && rawQuery != null)
            {
                query = rawQuery.ToString();
            }

            if (parameters.TryGetValue("limit", out var rawLimit) && rawLimit != null)
            {
                if (int.TryParse(rawLimit.ToString(), out var parsedLimit) && parsedLimit > 0)
                {
                    limit = Math.Min(parsedLimit, 100);
                }
            }
        }

        var results = await _noteRepository.SearchAsync(query, limit, cancellationToken).ConfigureAwait(false);
        return CommandResult.Success(results);
    }
}
