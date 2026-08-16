using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to create a new structured Roadmap with optional initial milestones and goal association.
/// </summary>
public class RoadmapCreateCommand : ICommand
{
    private readonly IRoadmapRepository _roadmapRepository;
    private readonly IGoalRepository _goalRepository;

    public const string CommandId = "roadmap.create";

    public RoadmapCreateCommand(IRoadmapRepository roadmapRepository, IGoalRepository goalRepository)
    {
        _roadmapRepository = roadmapRepository ?? throw new ArgumentNullException(nameof(roadmapRepository));
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
    }

    public string Id => CommandId;

    public string Name => "Crear Roadmap";

    public string Description => "Crea una nueva ruta de aprendizaje o plan secuencial en etapas e hitos.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("title", typeof(string), "Título descriptivo del Roadmap", IsRequired: true),
        new("description", typeof(string), "Detalle o contexto del Roadmap", IsRequired: false),
        new("goal_id", typeof(string), "Identificador del Goal asociado opcional", IsRequired: false),
        new("milestones", typeof(IEnumerable<string>), "Lista opcional de títulos de hitos iniciales", IsRequired: false)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.TryGetValue("title", out var rawTitle) || rawTitle == null || string.IsNullOrWhiteSpace(rawTitle.ToString()))
        {
            return CommandResult.Failure("El título del Roadmap es obligatorio.");
        }

        var title = rawTitle.ToString()!.Trim();

        string? description = null;
        if (parameters.TryGetValue("description", out var rawDesc) && rawDesc != null)
        {
            description = rawDesc.ToString()?.Trim();
        }

        string? goalId = null;
        if (parameters.TryGetValue("goal_id", out var rawGoalId) && rawGoalId != null)
        {
            var gId = rawGoalId.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(gId))
            {
                var existingGoal = await _goalRepository.GetByIdAsync(gId, cancellationToken).ConfigureAwait(false);
                if (existingGoal != null)
                {
                    goalId = gId;
                }
            }
        }

        var milestoneList = new List<RoadmapMilestone>();
        var roadmapId = Guid.NewGuid().ToString("N");

        if (parameters.TryGetValue("milestones", out var rawMilestones) && rawMilestones != null)
        {
            if (rawMilestones is IEnumerable<string> strList)
            {
                var index = 0;
                foreach (var mTitle in strList)
                {
                    if (!string.IsNullOrWhiteSpace(mTitle))
                    {
                        milestoneList.Add(new RoadmapMilestone
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            RoadmapId = roadmapId,
                            Title = mTitle.Trim(),
                            OrderIndex = index++,
                            Status = "pending",
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
        }

        var roadmap = new Roadmap
        {
            Id = roadmapId,
            GoalId = goalId,
            Title = title,
            Description = description,
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Milestones = milestoneList
        };

        try
        {
            await _roadmapRepository.CreateAsync(roadmap, milestoneList, cancellationToken).ConfigureAwait(false);
            return CommandResult.Success(roadmap);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Error al crear el Roadmap: {ex.Message}");
        }
    }
}
