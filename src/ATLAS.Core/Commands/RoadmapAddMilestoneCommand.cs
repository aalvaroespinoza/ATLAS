using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to add an ordered milestone step to an existing Roadmap.
/// </summary>
public class RoadmapAddMilestoneCommand : ICommand
{
    private readonly IRoadmapRepository _roadmapRepository;

    public const string CommandId = "roadmap.add_milestone";

    public RoadmapAddMilestoneCommand(IRoadmapRepository roadmapRepository)
    {
        _roadmapRepository = roadmapRepository ?? throw new ArgumentNullException(nameof(roadmapRepository));
    }

    public string Id => CommandId;

    public string Name => "Agregar Hito a Roadmap";

    public string Description => "Añade un nuevo paso o etapa secuencial dentro de un Roadmap existente.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("roadmap_id", typeof(string), "Identificador del Roadmap", IsRequired: true),
        new("title", typeof(string), "Título del hito o paso", IsRequired: true),
        new("order_index", typeof(int), "Orden del hito (opcional, por defecto al final)", IsRequired: false),
        new("notes", typeof(string), "Notas o recursos de referencia opcionales", IsRequired: false)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.TryGetValue("roadmap_id", out var rawRoadmapId) || rawRoadmapId == null || string.IsNullOrWhiteSpace(rawRoadmapId.ToString()))
        {
            return CommandResult.Failure("El ID del Roadmap es obligatorio.");
        }

        if (!parameters.TryGetValue("title", out var rawTitle) || rawTitle == null || string.IsNullOrWhiteSpace(rawTitle.ToString()))
        {
            return CommandResult.Failure("El título del hito es obligatorio.");
        }

        var roadmapId = rawRoadmapId.ToString()!.Trim();
        var title = rawTitle.ToString()!.Trim();

        var roadmap = await _roadmapRepository.GetByIdAsync(roadmapId, cancellationToken).ConfigureAwait(false);
        if (roadmap == null)
        {
            return CommandResult.Failure($"No se encontró el Roadmap con ID '{roadmapId}'.");
        }

        var orderIndex = roadmap.Milestones.Count;
        if (parameters.TryGetValue("order_index", out var rawOrder) && rawOrder != null)
        {
            if (rawOrder is int o) orderIndex = o;
            else if (int.TryParse(rawOrder.ToString(), out var parsedO)) orderIndex = parsedO;
        }

        string? notes = null;
        if (parameters.TryGetValue("notes", out var rawNotes) && rawNotes != null)
        {
            notes = rawNotes.ToString()?.Trim();
        }

        var milestone = new RoadmapMilestone
        {
            Id = Guid.NewGuid().ToString("N"),
            RoadmapId = roadmapId,
            Title = title,
            OrderIndex = orderIndex,
            Status = "pending",
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await _roadmapRepository.AddMilestoneAsync(milestone, cancellationToken).ConfigureAwait(false);
            return CommandResult.Success(milestone);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Error al agregar el hito: {ex.Message}");
        }
    }
}
