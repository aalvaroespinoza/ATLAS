using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to complete or uncomplete a roadmap milestone, recalculating progress and syncing the associated Goal.
/// </summary>
public class RoadmapCompleteMilestoneCommand : ICommand
{
    private readonly IRoadmapRepository _roadmapRepository;
    private readonly IGoalRepository _goalRepository;

    public const string CommandId = "roadmap.complete_milestone";

    public RoadmapCompleteMilestoneCommand(IRoadmapRepository roadmapRepository, IGoalRepository goalRepository)
    {
        _roadmapRepository = roadmapRepository ?? throw new ArgumentNullException(nameof(roadmapRepository));
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
    }

    public string Id => CommandId;

    public string Name => "Completar Hito de Roadmap";

    public string Description => "Marca un hito como completado o pendiente y actualiza automáticamente el avance del Roadmap y del Goal asociado.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("milestone_id", typeof(string), "Identificador único del hito", IsRequired: true),
        new("completed", typeof(bool), "Estado de completitud (true para completado, false para pendiente)", IsRequired: false, DefaultValue: true)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.TryGetValue("milestone_id", out var rawId) || rawId == null || string.IsNullOrWhiteSpace(rawId.ToString()))
        {
            return CommandResult.Failure("El ID del hito es obligatorio.");
        }

        var milestoneId = rawId.ToString()!.Trim();
        var completed = true;

        if (parameters.TryGetValue("completed", out var rawCompleted) && rawCompleted != null)
        {
            if (rawCompleted is bool b) completed = b;
            else if (bool.TryParse(rawCompleted.ToString(), out var parsedB)) completed = parsedB;
        }

        var milestone = await _roadmapRepository.GetMilestoneByIdAsync(milestoneId, cancellationToken).ConfigureAwait(false);
        if (milestone == null)
        {
            return CommandResult.Failure($"No se encontró el hito con ID '{milestoneId}'.");
        }

        var newStatus = completed ? "completed" : "pending";
        var completedAt = completed ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;

        try
        {
            await _roadmapRepository.UpdateMilestoneStatusAsync(milestoneId, newStatus, completedAt, cancellationToken).ConfigureAwait(false);

            milestone.Status = newStatus;
            milestone.CompletedAt = completedAt;

            // Recalculate Roadmap and sync Goal progress if linked
            var parentRoadmap = await _roadmapRepository.GetByIdAsync(milestone.RoadmapId, cancellationToken).ConfigureAwait(false);
            var updatedProgress = parentRoadmap?.ProgressPercentage ?? 0;
            Goal? updatedGoal = null;

            if (parentRoadmap != null && !string.IsNullOrWhiteSpace(parentRoadmap.GoalId))
            {
                var goal = await _goalRepository.GetByIdAsync(parentRoadmap.GoalId, cancellationToken).ConfigureAwait(false);
                if (goal != null)
                {
                    var updatedStatus = updatedProgress >= 100 && goal.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
                        ? "completed"
                        : goal.Status;

                    var goalToSave = new Goal
                    {
                        Id = goal.Id,
                        Title = goal.Title,
                        Description = goal.Description,
                        Status = updatedStatus,
                        Progress = updatedProgress,
                        TargetDate = goal.TargetDate,
                        CreatedAt = goal.CreatedAt
                    };

                    updatedGoal = await _goalRepository.UpdateAsync(goalToSave, cancellationToken).ConfigureAwait(false);
                }
            }

            return CommandResult.Success(new
            {
                MilestoneId = milestoneId,
                Status = newStatus,
                RoadmapId = milestone.RoadmapId,
                RoadmapProgress = updatedProgress,
                LinkedGoalId = parentRoadmap?.GoalId,
                LinkedGoalProgress = updatedGoal?.Progress
            });
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Error al actualizar el estado del hito: {ex.Message}");
        }
    }
}
