using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to create a new habit definition in ATLAS.
/// </summary>
public class HabitCreateCommand : ICommand
{
    private readonly IHabitRepository _habitRepository;

    public const string CommandId = "habit.create";

    public HabitCreateCommand(IHabitRepository habitRepository)
    {
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
    }

    public string Id => CommandId;

    public string Name => "Crear Hábito";

    public string Description => "Crea la definición de un nuevo hábito personal con su frecuencia de repetición.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("name", typeof(string), "Nombre del hábito a formar", IsRequired: true),
        new("description", typeof(string), "Descripción o contexto opcional", IsRequired: false),
        new("frequency", typeof(string), "Frecuencia esperada ('daily', 'weekly:N', 'days:1,3,5')", IsRequired: false, DefaultValue: "daily")
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null ||
            !parameters.TryGetValue("name", out var rawName) ||
            rawName == null ||
            string.IsNullOrWhiteSpace(rawName.ToString()))
        {
            return CommandResult.Failure("El parámetro 'name' es obligatorio y no puede estar vacío.");
        }

        var name = rawName.ToString()!.Trim();

        string? description = null;
        if (parameters.TryGetValue("description", out var rawDesc) && rawDesc != null)
        {
            description = rawDesc.ToString()?.Trim();
        }

        var frequency = "daily";
        if (parameters.TryGetValue("frequency", out var rawFreq) && rawFreq != null)
        {
            var freqStr = rawFreq.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(freqStr))
            {
                frequency = freqStr;
            }
        }

        var habit = new Habit
        {
            Name = name,
            Description = description,
            Frequency = frequency,
            CreatedAt = DateTimeOffset.UtcNow
        };

        try
        {
            var created = await _habitRepository.CreateAsync(habit, cancellationToken).ConfigureAwait(false);
            return CommandResult.Success(created);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}
