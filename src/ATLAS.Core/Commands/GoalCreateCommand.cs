using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to create a new personal Goal in ATLAS.
/// </summary>
public class GoalCreateCommand : ICommand
{
    private readonly IGoalRepository _goalRepository;

    public const string CommandId = "goal.create";

    public GoalCreateCommand(IGoalRepository goalRepository)
    {
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
    }

    public string Id => CommandId;

    public string Name => "Crear Meta";

    public string Description => "Crea una nueva meta personal con estado activo y progreso inicial en 0.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("title", typeof(string), "Título u objetivo principal de la meta", IsRequired: true),
        new("description", typeof(string), "Descripción o contexto opcional", IsRequired: false),
        new("target_date", typeof(string), "Fecha objetivo límite opcional (formato ISO-8601)", IsRequired: false)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null ||
            !parameters.TryGetValue("title", out var rawTitle) ||
            rawTitle == null ||
            string.IsNullOrWhiteSpace(rawTitle.ToString()))
        {
            return CommandResult.Failure("El parámetro 'title' es obligatorio y no puede estar vacío.");
        }

        var title = rawTitle.ToString()!.Trim();

        string? description = null;
        if (parameters.TryGetValue("description", out var rawDesc) && rawDesc != null)
        {
            description = rawDesc.ToString()?.Trim();
        }

        DateTimeOffset? targetDate = null;
        if (parameters.TryGetValue("target_date", out var rawTarget) && rawTarget != null)
        {
            if (rawTarget is DateTimeOffset dto)
            {
                targetDate = dto;
            }
            else if (rawTarget is DateTime dt)
            {
                targetDate = new DateTimeOffset(dt);
            }
            else if (DateTimeOffset.TryParse(rawTarget.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                targetDate = parsed;
            }
        }

        var goal = new Goal
        {
            Title = title,
            Description = description,
            Status = "active",
            Progress = 0,
            TargetDate = targetDate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var created = await _goalRepository.CreateAsync(goal, cancellationToken).ConfigureAwait(false);
            return CommandResult.Success(created);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}
