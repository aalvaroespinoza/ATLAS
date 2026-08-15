using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to update the progress and/or status of an existing Goal.
/// </summary>
public class GoalUpdateProgressCommand : ICommand
{
    private readonly IGoalRepository _goalRepository;

    public const string CommandId = "goal.update_progress";

    public GoalUpdateProgressCommand(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
    }

    public string Id => CommandId;

    public string Name => "Actualizar Progreso de Meta";

    public string Description => "Actualiza el porcentaje de avance y/o estado de una meta existente.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("goal_id", typeof(string), "Identificador único de la meta a actualizar", IsRequired: true),
        new("progress", typeof(int), "Nuevo porcentaje de progreso (0 a 100)", IsRequired: true),
        new("status", typeof(string), "Estado opcional de la meta ('active', 'completed', 'paused', 'abandoned')", IsRequired: false)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null ||
            !parameters.TryGetValue("goal_id", out var rawGoalId) ||
            rawGoalId == null ||
            string.IsNullOrWhiteSpace(rawGoalId.ToString()))
        {
            return CommandResult.Failure("El parámetro 'goal_id' es obligatorio y no puede estar vacío.");
        }

        if (!parameters.TryGetValue("progress", out var rawProgress) ||
            rawProgress == null ||
            !int.TryParse(rawProgress.ToString(), out var progress))
        {
            return CommandResult.Failure("El parámetro 'progress' es obligatorio y debe ser un número entero entre 0 y 100.");
        }

        var goalId = rawGoalId.ToString()!.Trim();
        var clampedProgress = Math.Clamp(progress, 0, 100);

        string? explicitStatus = null;
        if (parameters.TryGetValue("status", out var rawStatus) && rawStatus != null)
        {
            var statusStr = rawStatus.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(statusStr))
            {
                explicitStatus = statusStr;
            }
        }

        try
        {
            var existingGoal = await _goalRepository.GetByIdAsync(goalId, cancellationToken).ConfigureAwait(false);
            if (existingGoal == null)
            {
                return CommandResult.Failure($"No se encontró la meta con ID '{goalId}'.");
            }

            var newStatus = explicitStatus ?? (clampedProgress >= 100 ? "completed" : existingGoal.Status);

            var updatedGoal = new Goal
            {
                Id = existingGoal.Id,
                Title = existingGoal.Title,
                Description = existingGoal.Description,
                Status = newStatus,
                Progress = clampedProgress,
                TargetDate = existingGoal.TargetDate,
                CreatedAt = existingGoal.CreatedAt
            };

            var saved = await _goalRepository.UpdateAsync(updatedGoal, cancellationToken).ConfigureAwait(false);
            return CommandResult.Success(saved);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}
